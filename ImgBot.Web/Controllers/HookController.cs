using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImgBot.Common.Mediation;
using ImgBot.Common.Messages;
using ImgBot.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace ImgBot.Web.Controllers
{
    public class HookController : Controller
    {
        private readonly IMediator _mediator;

        public HookController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromBody] Hook hook)
        {
            var hookEvent = HttpContext.Request.Headers["X-GitHub-Event"];

            if (hookEvent != "installation_repositories" && hookEvent != "installation" && hookEvent != "pull_request")
            {
                return Json(new { data = "Not supported." });
            }

            if (hook.installation.access_tokens_url == null && hookEvent != "pull_request")
            {
                return Json(new { data = "Installation access_tokens_url required" });
            }

            switch (hookEvent)
            {
                case "installation_repositories":
                    await ProcessInstallationRepositoriesAsync(hook);
                    break;
                case "installation":
                    await ProcessInstallationAsync(hook);
                    break;
                case "pull_request":
                    await ProcessPullRequestAsync(hook);
                    break;
            }

            return Json(new { data = true });
        }

        private Task ProcessPullRequestAsync(Hook hook)
        {
            throw new NotImplementedException();
        }

        private async Task ProcessInstallationAsync(Hook hook)
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
                case "deleted":
                    await Task.FromResult(0);
                    break;

            }
        }

        private Task ProcessInstallationRepositoriesAsync(Hook hook)
        {
            throw new NotImplementedException();
        }
    }
}
