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
            var storageAccount = CloudStorageAccount.Parse(KnownEnvironmentVariables.AzureWebJobsStorage);
            var routerQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("routermessage");
            var openPrQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("openprmessage");
            var deleteBranchMessages = storageAccount.CreateCloudQueueClient().GetQueueReference("deletebranchmessage");
            var installationTable = storageAccount.CreateCloudTableClient().GetTableReference("installation");
            var marketplaceTable = storageAccount.CreateCloudTableClient().GetTableReference("marketplace");
            var settingsTable = storageAccount.CreateCloudTableClient().GetTableReference("settings");

            return Run(
                req,
                routerQueue,
                openPrQueue,
                deleteBranchMessages,
                installationTable,
                marketplaceTable,
                settingsTable,
                logger);
        }

        public static async Task<IActionResult> Run(
            HttpRequestMessage req,
            CloudQueue routerMessages,
            CloudQueue openPrMessages,
            CloudQueue deleteBranchMessages,
            CloudTable installationTable,
            CloudTable marketplaceTable,
            CloudTable settingsTable,
            ILogger logger)
        {
            var hookEvent = req.Headers.GetValues("X-GitHub-Event").First();
            var hook = JsonConvert.DeserializeObject<Hook>(await req.Content.ReadAsStringAsync());
            var result = "no action";
            switch (hookEvent)
            {
                case "installation_repositories":
                case "installation":
                    result = await ProcessInstallationAsync(hook, marketplaceTable, routerMessages, installationTable, logger).ConfigureAwait(false);
                    break;
                case "push":
                    result = await ProcessPushAsync(hook, marketplaceTable, settingsTable, routerMessages, openPrMessages, deleteBranchMessages, logger)
                                    .ConfigureAwait(false);
                    break;
                case "marketplace_purchase":
                    result = await ProcessMarketplacePurchaseAsync(hook, marketplaceTable, logger).ConfigureAwait(false);
                    break;
            }

            return new OkObjectResult(new HookResponse { Result = result });
        }

        private static async Task<string> ProcessPushAsync(
            Hook hook,
            CloudTable marketplaceTable,
            CloudTable settingsTable,
            CloudQueue routerMessages,
            CloudQueue openPrMessages,
            CloudQueue deleteBranchMessages,
            ILogger logger)
        {
            // private check
            if (hook.repository?.@private == true)
            {
                var isPrivateEligible = await IsPrivateEligible(marketplaceTable, hook.repository.owner.login);
                if (!isPrivateEligible)
                {
                    logger.LogError("ProcessPush: Plan mismatch for {Owner}/{RepoName}", hook.repository.owner.login, hook.repository.name);
                    throw new Exception("Plan mismatch");
                }
            }

            // push to imgbot branch by imgbot
            if (hook.@ref == $"refs/heads/{KnownGitHubs.BranchName}" && hook.sender.login == "imgbot[bot]")
            {
                await openPrMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new OpenPrMessage
                {
                    InstallationId = hook.installation.id,
                    RepoName = hook.repository.name,
                    CloneUrl = $"https://github.com/{hook.repository.full_name}",
                    Update = false,
                })));

                logger.LogInformation("ProcessPush: Added OpenPrMessage for {Owner}/{RepoName}", hook.repository.owner.login, hook.repository.name);

                return "imgbot push";
            }

            var repositorySettings = await SettingsHelper.GetSettings(settingsTable, hook.installation.id, hook.repository.name);
            var branchToCheck = hook.repository.default_branch;
            if (repositorySettings != null && !string.IsNullOrEmpty(repositorySettings.DefaultBranchOverride))
            {
                logger.LogInformation(
                    "ProcessPush: default branch override for {Owner}/{RepoName} is {DefaultBranchOverride}",
                    hook.repository.owner.login,
                    hook.repository.name,
                    repositorySettings.DefaultBranchOverride);
                branchToCheck = repositorySettings.DefaultBranchOverride;
            }

            // push to non-default branch
            if (hook.@ref != $"refs/heads/{branchToCheck}")
            {
                return "Commit to non default branch (or override)";
            }

            // merge commit to default branch from imgbot branch
            if (IsDefaultWebMerge(hook, branchToCheck))
            {
                await deleteBranchMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new DeleteBranchMessage
                {
                    InstallationId = hook.installation.id,
                    RepoName = hook.repository.name,
                    Owner = hook.repository.owner.login,
                    CloneUrl = $"https://github.com/{hook.repository.full_name}",
                })));

                return "deleteit";
            }

            // regular commit to default branch
            var relevantFiles = hook.commits.SelectMany(x => x.added)
                .Concat(hook.commits.SelectMany(x => x.modified));
            var imageFiles = relevantFiles.Where(file => KnownImgPatterns.ImgExtensions.Any(extension => file.ToLower().EndsWith(extension, StringComparison.Ordinal)));
            var configFile = relevantFiles.Where(file => file.ToLower() == ".imgbotconfig");

            if (!imageFiles.Any() && !configFile.Any())
            {
                return "No relevant files touched";
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

        private static async Task<string> ProcessInstallationAsync(Hook hook, CloudTable marketplaceTable, CloudQueue routerMessages, CloudTable installationTable, ILogger logger)
        {
            var isPrivateEligible = false;
            var isOnAddedPlan = false;
            int? usedPrivate = 0;
            int? allowedPrivate = 0;
            var privateRepo = hook.repositories?.Any(x => x.@private) == true || hook.repositories_added?.Any(x => x.@private) == true;
            if (privateRepo)
            {
                (isOnAddedPlan, allowedPrivate, usedPrivate) = await IsOnAddedPlan(marketplaceTable, hook.installation.account.login);
            }

            if (!isOnAddedPlan && privateRepo)
            {
                isPrivateEligible = await IsPrivateEligible(marketplaceTable, hook.installation.account.login);
            }

            switch (hook.action)
            {
                case "created":

                    foreach (var repo in hook.repositories)
                    {
                        if (repo.@private && !isPrivateEligible && !isOnAddedPlan)
                        {
                            logger.LogError("ProcessInstallationAsync/added: Plan mismatch for {Owner}/{RepoName}", hook.installation.account.login, repo.name);
                            continue;
                        }

                        var compress = true;
                        if (repo.@private && isOnAddedPlan)
                        {
                            compress = false;
                            if (usedPrivate < allowedPrivate)
                            {
                                usedPrivate++;
                                await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(new Marketplace(hook.installation.account.id, hook.installation.account.login)
                                {
                                    UsedPrivate = usedPrivate,
                                }));
                                compress = true;
                            }
                        }

                        await routerMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new RouterMessage
                        {
                            InstallationId = hook.installation.id,
                            Owner = hook.installation.account.login,
                            RepoName = repo.name,
                            CloneUrl = $"https://github.com/{repo.full_name}",
                            IsPrivate = repo.@private,
                            Compress = compress,
                        })));

                        logger.LogInformation("ProcessInstallationAsync/created: Added RouterMessage for {Owner}/{RepoName}", hook.installation.account.login, repo.name);
                    }

                    break;
                case "added":

                    foreach (var repo in hook.repositories_added)
                    {
                        if (repo.@private && !isPrivateEligible && !isOnAddedPlan)
                        {
                            logger.LogError("ProcessInstallationAsync/added: Plan mismatch for {Owner}/{RepoName}", hook.installation.account.login, repo.name);
                            continue;
                        }

                        var compress = true;
                        if (repo.@private && isOnAddedPlan)
                        {
                            compress = false;
                            if (usedPrivate < allowedPrivate)
                            {
                                usedPrivate++;
                                await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(new Marketplace(hook.installation.account.id, hook.installation.account.login)
                                {
                                    UsedPrivate = usedPrivate,
                                }));
                                compress = true;
                            }
                        }

                        await routerMessages.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new RouterMessage
                        {
                            InstallationId = hook.installation.id,
                            Owner = hook.installation.account.login,
                            RepoName = repo.name,
                            CloneUrl = $"https://github.com/{repo.full_name}",
                            IsPrivate = repo.@private,
                            Compress = compress,
                        })));

                        logger.LogInformation("ProcessInstallationAsync/added: Added RouterMessage for {Owner}/{RepoName}", hook.installation.account.login, repo.name);
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
                    int? allowedPrivate = null;
                    var limitedPlans = KnownGitHubs.Plans.Keys.Where(k => KnownGitHubs.Plans[k] >= KnownGitHubs.SmallestLimitPaidPlan);
                    if (limitedPlans.Contains(hook.marketplace_purchase.plan.id))
                    {
                        allowedPrivate = KnownGitHubs.Plans[hook.marketplace_purchase.plan.id];
                    }

                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(new Marketplace(hook.marketplace_purchase.account.id, hook.marketplace_purchase.account.login)
                    {
                        AccountType = hook.marketplace_purchase.account.type,
                        SenderEmail = hook.sender.email,
                        OrganizationBillingEmail = hook.marketplace_purchase.account.organization_billing_email,
                        PlanId = hook.marketplace_purchase.plan.id,
                        SenderId = hook.sender.id,
                        SenderLogin = hook.sender.login,
                        AllowedPrivate = allowedPrivate,
                        UsedPrivate = 0,
                    }));

                    logger.LogInformation("ProcessMarketplacePurchaseAsync/purchased {PlanId} for {Owner}", hook.marketplace_purchase.plan.id, hook.marketplace_purchase.account.login);

                    return hook.action;
                case "cancelled":
                    await marketplaceTable.DropRow(hook.marketplace_purchase.account.id, hook.marketplace_purchase.account.login);
                    logger.LogInformation("ProcessMarketplacePurchaseAsync/cancelled {PlanId} for {Owner}", hook.marketplace_purchase.plan.id, hook.marketplace_purchase.account.login);
                    return "cancelled";
                default:
                    return hook.action;
            }
        }

        private static async Task<bool> IsPrivateEligible(CloudTable marketplaceTable, string ownerLogin)
        {
            var unlimitedPlans = KnownGitHubs.Plans.Keys.Where(k => KnownGitHubs.Plans[k] == -1 || KnownGitHubs.Plans[k] == -2);
            string plansQuery = string.Empty;

            foreach (int planId in unlimitedPlans)
            {
                plansQuery += "PlanId eq " + planId.ToString() + " or ";
            }

            var query = new TableQuery<Marketplace>().Where(
                    $"AccountLogin eq '{ownerLogin}' and ({plansQuery} Student eq true)");

            var rows = await marketplaceTable.ExecuteQuerySegmentedAsync(query, null);
            return rows.Count() != 0;
        }

        private static async Task<(bool isOnAddedPlan, int? allowedPrivate, int? usedPrivate)> IsOnAddedPlan(CloudTable marketplaceTable, string ownerLogin)
        {
            var limitedPlans = KnownGitHubs.Plans.Keys.Where(k => KnownGitHubs.Plans[k] >= KnownGitHubs.SmallestLimitPaidPlan);
            string plansQuery = string.Empty;
            string needsOr = string.Empty;
            if (limitedPlans.Count() > 0)
            {
                needsOr = " or";
            }

            int i = 0;
            foreach (int planId in limitedPlans)
            {
                plansQuery += "PlanId eq " + planId.ToString();
                if (i != limitedPlans.Count() - 1)
                {
                    plansQuery += needsOr;
                }

                i++;
            }

            var query = new TableQuery<Marketplace>().Where(
                    $"AccountLogin eq '{ownerLogin}' and ({plansQuery})");

            var rows = await marketplaceTable.ExecuteQuerySegmentedAsync(query, null);

            var plan = rows.FirstOrDefault();
            if (plan != null)
            {
               return (isOnAddedPlan: true, allowedPrivate: plan.AllowedPrivate, usedPrivate: plan.UsedPrivate);
            }

            return (isOnAddedPlan: false, allowedPrivate: 0, usedPrivate: 0);
        }

        // We are using commit hooks here, so let's deduce whether this is an eligble scenario for auto-deleting a branch
        // 1. should be merged using the web gui on github.com
        // 2. should be merging into the default branch from the imgbot branch
        // 3. should only contain the merge commit and the imgbot commit to be eligible
        private static bool IsDefaultWebMerge(Hook hook, string branchToCheck)
        {
            if (hook.@ref != $"refs/heads/{branchToCheck}")
                return false;

            if (hook.commits?.Count == 1)
            {
                // squash?
                if (hook.commits?[0]?.author?.username != "imgbot[bot]" && hook.commits?[0]?.author?.username != "ImgBotApp")
                    return false;
            }
            else
            {
                // regular merge?
                if (hook.head_commit?.committer?.username != "web-flow")
                    return false;
                if (hook.commits?.Count > 2)
                    return false;
                if (hook.commits?.All(x => x.committer.username != "ImgBotApp") == true)
                    return false;
            }

            return true;
        }
    }
}
