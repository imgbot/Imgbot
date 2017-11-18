using System.Linq;
using System.Threading.Tasks;
using ImgBot.Common;
using ImgBot.Common.Mediation;
using ImgBot.Common.Messages;
using ImgBot.Common.Repository;
using ImgBot.Common.TableModels;
using ImgBot.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImgBot.Web.Controllers
{
    public class HookController : Controller
    {
        private readonly IMediator _mediator;
        private readonly IRepository _repository;

        public HookController(IMediator mediator, IRepository repository)
        {
            _mediator = mediator;
            _repository = repository;
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromBody] Hook hook)
        {
            var hookEvent = HttpContext.Request.Headers["X-GitHub-Event"];

            string response = "false";

            switch (hookEvent)
            {
                case "installation_repositories":
                case "installation":
                case "integration_installation_repositories":
                case "integration_installation":
                    response = await ProcessInstallationAsync(hook);
                    break;
                case "push":
                    response = await ProcessPushAsync(hook);
                    break;
            }

            return Json(new { data = response });
        }

        private async Task<string> ProcessPushAsync(Hook hook)
        {
            if (hook.@ref != "refs/heads/master")
            {
                return "Commit to non master branch";
            }

            var files = hook.commits.SelectMany(x => x.added)
                .Concat(hook.commits.SelectMany(x => x.modified))
                .Where(file => KnownImgPatterns.ImgExtensions.Any(extension => file.EndsWith(extension)));

            if (files.Any() == false)
            {
                return "No image files touched";
            }

            await _mediator.SendAsync(new ImageUpdateMessage
            {
                InstallationId = hook.installation.id,
                RepoName = hook.repository.name,
            });

            return "true";
        }

        private async Task<string> ProcessInstallationAsync(Hook hook)
        {
            switch (hook.action)
            {
                case "created":
                    foreach (var repo in hook.repositories)
                    {
                        await _mediator.SendAsync(new InstallationMessage
                        {
                            InstallationId = hook.installation.id,
                            Owner = hook.installation.account.login,
                            AccessTokensUrl = hook.installation.access_tokens_url,
                            RepoName = repo.name,
                            CloneUrl = $"https://github.com/{repo.full_name}",
                        });
                    }

                    break;

                case "added":
                    foreach (var repo in hook.repositories_added)
                    {
                        await _mediator.SendAsync(new InstallationMessage
                        {
                            InstallationId = hook.installation.id,
                            Owner = hook.installation.account.login,
                            AccessTokensUrl = hook.installation.access_tokens_url,
                            RepoName = repo.name,
                            CloneUrl = $"https://github.com/{repo.full_name}",
                        });
                    }

                    break;

                case "removed":
                    foreach (var repo in hook.repositories_removed)
                    {
                        await _repository.DeleteAsync<Installation>(hook.installation.id.ToString(), repo.name);
                    }

                    break;

                case "deleted":
                    var installations = await _repository.RetrievePartitionAsync<Installation>(hook.installation.id.ToString());
                    foreach (var installation in installations)
                    {
                        await _repository.DeleteAsync<Installation>(hook.installation.id.ToString(), installation.RepoName);
                    }

                    break;
            }

            return "true";
        }
    }
}
