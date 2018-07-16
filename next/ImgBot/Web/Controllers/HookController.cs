using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Mediation;
using Common.Messages;
using Common.Repository;
using Common.TableModels;
using Microsoft.AspNetCore.Mvc;
using Web.Models;

namespace Web.Controllers
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
                case "marketplace_purchase":
                    response = await ProcessMarketplacePurchaseAsync(hook);
                    break;
            }

            return Json(new { data = response });
        }

        private async Task<string> ProcessPushAsync(Hook hook)
        {
            if (hook.@ref != $"refs/heads/{hook.repository.default_branch}")
            {
                return "Commit to non default branch";
            }

            var files = hook.commits.SelectMany(x => x.added)
                .Concat(hook.commits.SelectMany(x => x.modified))
                .Where(file => KnownImgPatterns.ImgExtensions.Any(extension => file.EndsWith(extension, StringComparison.Ordinal)));

            if (files.Any() == false)
            {
                return "No image files touched";
            }

            await _mediator.SendAsync(new RouterMessage
            {
                InstallationId = hook.installation.id,
                Owner = hook.installation.account.login,
                AccessTokensUrl = hook.installation.access_tokens_url,
                RepoName = hook.repository.name,
                CloneUrl = $"https://github.com/{hook.repository.full_name}",
            });

            return "true";
        }

        private async Task<string> ProcessInstallationAsync(Hook hook)
        {
            switch (hook.action)
            {
                case "created":
                    await Task.WhenAll(hook.repositories.Select(repo => _mediator.SendAsync(new RouterMessage
                    {
                        InstallationId = hook.installation.id,
                        Owner = hook.installation.account.login,
                        AccessTokensUrl = hook.installation.access_tokens_url,
                        RepoName = repo.name,
                        CloneUrl = $"https://github.com/{repo.full_name}",
                    })));

                    break;

                case "added":
                    await Task.WhenAll(hook.repositories_added.Select(repo => _mediator.SendAsync(new RouterMessage
                    {
                        InstallationId = hook.installation.id,
                        Owner = hook.installation.account.login,
                        AccessTokensUrl = hook.installation.access_tokens_url,
                        RepoName = repo.name,
                        CloneUrl = $"https://github.com/{repo.full_name}",
                    })));

                    break;

                case "removed":
                    await Task.WhenAll(hook.repositories_removed.Select(repo => _repository.DeleteAsync<Installation>(hook.installation.id.ToString(), repo.name)));

                    break;

                case "deleted":
                    var installations = await _repository.RetrievePartitionAsync<Installation>(hook.installation.id.ToString());
                    await Task.WhenAll(installations.Select(installation => _repository.DeleteAsync<Installation>(hook.installation.id.ToString(), installation.RepoName)));

                    break;
            }

            return "true";
        }

        private async Task<string> ProcessMarketplacePurchaseAsync(Hook hook)
        {
            switch (hook.action)
            {
                case "purchased":
                    await _repository.InsertOrMergeAsync(new Marketplace(hook.marketplace_purchase.account.id, hook.marketplace_purchase.account.login)
                    {
                        AccountType = hook.marketplace_purchase.account.type,
                        PlanId = hook.marketplace_purchase.plan.id,
                        SenderId = hook.sender.id,
                        SenderLogin = hook.sender.login,
                    });
                    return "true";
                case "cancelled":
                    await _repository.DeleteAsync<Marketplace>(hook.marketplace_purchase.account.id.ToString(), hook.marketplace_purchase.account.login);
                    return "true";
                default:
                    return hook.action;
            }
        }
    }
}
