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
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using WebHook.Model;

namespace WebHook
{
    public static class WebHookFunction
    {
        [FunctionName("WebHookFunction")]
        public static Task<IActionResult> Hook(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "hook")]HttpRequestMessage req,
        ILogger logger)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var routerQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("routermessage");
            var openPrQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("openprmessage");
            var installationTable = storageAccount.CreateCloudTableClient().GetTableReference("installation");
            var marketplaceTable = storageAccount.CreateCloudTableClient().GetTableReference("marketplace");

            return Run(req, routerQueue, openPrQueue, installationTable, marketplaceTable, logger);
        }

        public static async Task<IActionResult> Run(
            HttpRequestMessage req,
            CloudQueue routerMessages,
            CloudQueue openPrMessages,
            CloudTable installationTable,
            CloudTable marketplaceTable,
            ILogger logger)
        {
            var hookEvent = req.Headers.GetValues("X-GitHub-Event").First();
            var hook = JsonConvert.DeserializeObject<Hook>(await req.Content.ReadAsStringAsync());

            var result = "no action";

            if (hook.repository?.@private == true)
            {
                var query = new TableQuery<Marketplace>().Where(
                    $"AccountLogin eq '{hook.repository.owner.login}' and (PlanId eq 1750 or PlanId eq 781)");
                var rows = await marketplaceTable.ExecuteQuerySegmentedAsync(query, null);
                if (rows.Count() == 0)
                {
                    logger.LogError("ProcessPush: Plan mismatch for {Owner}/{RepoName}", hook.repository.owner.login, hook.repository.name);
                    throw new Exception("Plan mismatch");
                }
            }

            switch (hookEvent)
            {
                case "installation_repositories":
                case "installation":
                    result = await ProcessInstallationAsync(hook, routerMessages, installationTable, logger).ConfigureAwait(false);
                    break;
                case "push":
                    result = await ProcessPushAsync(hook, routerMessages, openPrMessages, logger).ConfigureAwait(false);
                    break;
                case "marketplace_purchase":
                    result = await ProcessMarketplacePurchaseAsync(hook, marketplaceTable, logger).ConfigureAwait(false);
                    break;
            }

            return new OkObjectResult(new HookResponse { Result = result });
        }

        private static async Task<string> ProcessPushAsync(Hook hook, CloudQueue routerMessages, CloudQueue openPrMessages, ILogger logger)
        {
            if (hook.@ref == $"refs/heads/{KnownGitHubs.BranchName}" && hook.sender.login == "imgbot[bot]")
            {
                await openPrMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new OpenPrMessage
                {
                    InstallationId = hook.installation.id,
                    RepoName = hook.repository.name,
                    CloneUrl = $"https://github.com/{hook.repository.full_name}",
                })));

                logger.LogInformation("ProcessPush: Added OpenPrMessage for {Owner}/{RepoName}", hook.repository.owner.login, hook.repository.name);

                return "imgbot push";
            }

            if (hook.@ref != $"refs/heads/{hook.repository.default_branch}")
            {
                return "Commit to non default branch";
            }

            var files = hook.commits.SelectMany(x => x.added)
                .Concat(hook.commits.SelectMany(x => x.modified))
                .Where(file => KnownImgPatterns.ImgExtensions.Any(extension => file.ToLower().EndsWith(extension, StringComparison.Ordinal)));

            if (files.Any() == false)
            {
                return "No image files touched";
            }

            await routerMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new RouterMessage
            {
                InstallationId = hook.installation.id,
                Owner = hook.repository.owner.login,
                RepoName = hook.repository.name,
                CloneUrl = $"https://github.com/{hook.repository.full_name}",
            })));

            logger.LogInformation("ProcessPush: Added RouterMessage for {Owner}/{RepoName}", hook.repository.owner.login, hook.repository.name);

            return "truth";
        }

        private static async Task<string> ProcessInstallationAsync(Hook hook, CloudQueue routerMessages, CloudTable installationTable, ILogger logger)
        {
            switch (hook.action)
            {
                case "created":
                    foreach (var repo in hook.repositories)
                    {
                        await routerMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new RouterMessage
                        {
                            InstallationId = hook.installation.id,
                            Owner = hook.installation.account.login,
                            RepoName = repo.name,
                            CloneUrl = $"https://github.com/{repo.full_name}",
                        })));

                        logger.LogInformation("ProcessInstallationAsync/created: Added RouterMessage for {Owner}/{RepoName}", repo.owner, repo.name);
                    }

                    break;
                case "added":
                    foreach (var repo in hook.repositories_added)
                    {
                        await routerMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new RouterMessage
                        {
                            InstallationId = hook.installation.id,
                            Owner = hook.installation.account.login,
                            RepoName = repo.name,
                            CloneUrl = $"https://github.com/{repo.full_name}",
                        })));

                        logger.LogInformation("ProcessInstallationAsync/added: Added RouterMessage for {Owner}/{RepoName}", repo.owner, repo.name);
                    }

                    break;
                case "removed":
                    foreach (var repo in hook.repositories_removed)
                    {
                        await installationTable.DropRow(hook.installation.id.ToString(), repo.name);
                        logger.LogInformation("ProcessInstallationAsync/removed: DropRow for {InstallationId} :: {RepoName}", hook.installation.id, repo.name);
                    }

                    break;
                case "deleted":
                    await installationTable.DropPartitionAsync(hook.installation.id.ToString());
                    logger.LogInformation("ProcessInstallationAsync/deleted: DropPartition for {InstallationId}", hook.installation.id);
                    break;
            }

            return "truth";
        }

        private static async Task<string> ProcessMarketplacePurchaseAsync(Hook hook, CloudTable marketplaceTable, ILogger logger)
        {
            switch (hook.action)
            {
                case "changed":
                case "purchased":
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(new Marketplace(hook.marketplace_purchase.account.id, hook.marketplace_purchase.account.login)
                    {
                        AccountType = hook.marketplace_purchase.account.type,
                        PlanId = hook.marketplace_purchase.plan.id,
                        SenderId = hook.sender.id,
                        SenderLogin = hook.sender.login,
                    }));

                    logger.LogInformation("ProcessMarketplacePurchaseAsync/purchased for {Owner}", hook.marketplace_purchase.account.login);

                    return hook.action;
                case "cancelled":
                    await marketplaceTable.DropRow(hook.marketplace_purchase.account.id, hook.marketplace_purchase.account.login);
                    logger.LogInformation("ProcessMarketplacePurchaseAsync/cancelled for {Owner}", hook.marketplace_purchase.account.login);
                    return "cancelled";
                default:
                    return hook.action;
            }
        }
    }
}
