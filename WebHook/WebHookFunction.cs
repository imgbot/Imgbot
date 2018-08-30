using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using WebHook.Model;

namespace WebHook
{
    public static class WebHookFunction
    {
        [FunctionName("WebHookFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "hook")]HttpRequestMessage req,
            [Queue("routermessage")] ICollector<RouterMessage> routerMessages,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            [Table("installation")] CloudTable installationTable,
            [Table("marketplace")] CloudTable marketplaceTable,
            TraceWriter log)
        {
            var hookEvent = req.Headers.GetValues("X-GitHub-Event").First();
            var hook = JsonConvert.DeserializeObject<Hook>(await req.Content.ReadAsStringAsync());

            var result = "no action";

            switch (hookEvent)
            {
                case "installation_repositories":
                case "installation":
                case "integration_installation_repositories":
                case "integration_installation":
                    result = await ProcessInstallationAsync(hook, routerMessages, installationTable);
                    break;
                case "push":
                    result = ProcessPush(hook, routerMessages, openPrMessages);
                    break;
                case "marketplace_purchase":
                    result = await ProcessMarketplacePurchaseAsync(hook, marketplaceTable);
                    break;
            }

            return new OkObjectResult(new HookResponse { Result = result });
        }

        private static string ProcessPush(Hook hook, ICollector<RouterMessage> routerMessages, ICollector<OpenPrMessage> openPrMessages)
        {
            if (hook.@ref == $"refs/heads/{KnownGitHubs.BranchName}")
            {
                openPrMessages.Add(new OpenPrMessage
                {
                    InstallationId = hook.installation.id,
                    RepoName = hook.repository.name,
                    CloneUrl = $"https://github.com/{hook.repository.full_name}",
                });

                return "imgbot push";
            }

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

            routerMessages.Add(new RouterMessage
            {
                InstallationId = hook.installation.id,
                Owner = hook.repository.owner.login,
                AccessTokensUrl = $"https://api.github.com/installations/{hook.installation.id}/access_tokens", // access_tokens url not available from this hook :(
                RepoName = hook.repository.name,
                CloneUrl = $"https://github.com/{hook.repository.full_name}",
            });

            return "truth";
        }

        private static async Task<string> ProcessInstallationAsync(Hook hook, ICollector<RouterMessage> routerMessages, CloudTable installationTable)
        {
            switch (hook.action)
            {
                case "created":
                    foreach (var repo in hook.repositories)
                    {
                        routerMessages.Add(new RouterMessage
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
                        routerMessages.Add(new RouterMessage
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
                        await installationTable.DropRow(hook.installation.id.ToString(), repo.name);
                    }

                    break;
                case "deleted":
                    await installationTable.DropPartitionAsync(hook.installation.id.ToString());
                    break;
            }

            return "truth";
        }

        private static async Task<string> ProcessMarketplacePurchaseAsync(Hook hook, CloudTable marketplaceTable)
        {
            switch (hook.action)
            {
                case "purchased":
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(new Marketplace(hook.marketplace_purchase.account.id, hook.marketplace_purchase.account.login)
                    {
                        AccountType = hook.marketplace_purchase.account.type,
                        PlanId = hook.marketplace_purchase.plan.id,
                        SenderId = hook.sender.id,
                        SenderLogin = hook.sender.login,
                    }));

                    return "purchased";
                case "cancelled":
                    await marketplaceTable.DropRow(hook.marketplace_purchase.account.id.ToString(), hook.marketplace_purchase.account.login);
                    return "cancelled";
                default:
                    return hook.action;
            }
        }
    }
}
