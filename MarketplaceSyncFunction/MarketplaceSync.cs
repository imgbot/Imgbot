using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using Octokit;

namespace MarketplaceSyncFunction
{
    public static class MarketplaceSync
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [Singleton]
        [FunctionName("MarketplaceSync")]
        public static async Task TimerTrigger(
            [TimerTrigger("0 0 * * * *", RunOnStartup = true)]TimerInfo timerInfo,
            ILogger logger,
            ExecutionContext context)
        {
            var installationTokenProvider = new InstallationTokenProvider();
            var storageAccount = CloudStorageAccount.Parse(KnownEnvironmentVariables.AzureWebJobsStorage);
            var marketplaceTable = storageAccount.CreateCloudTableClient().GetTableReference("marketplace");
            await RunAsync(marketplaceTable, installationTokenProvider, logger, context);
        }

        public static async Task RunAsync(
            CloudTable marketplaceTable,
            IInstallationTokenProvider installationTokenProvider,
            ILogger logger,
            ExecutionContext context)
        {
            logger.LogInformation("MarketplaceSync starting");
            var jwt = installationTokenProvider.GenerateJWT(
                new InstallationTokenParameters
                {
                    AppId = KnownGitHubs.AppId,
                },
                KnownEnvironmentVariables.APP_PRIVATE_KEY);
            var currentPlans = KnownGitHubs.Plans.Keys.Where(k =>
                KnownGitHubs.Plans[k] == -1 || KnownGitHubs.Plans[k] >= KnownGitHubs.SmallestLimitPaidPlan);

            foreach (var planId in currentPlans)
            {
                // TODO this will need to be updated to get more than 100 purchases per plan (loop with page) when we get more of the new plans bought
                var planRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/marketplace_listing/plans/{planId}/accounts?per_page=100");
                planRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                planRequest.Headers.Add("User-Agent", "ImgBot");
                planRequest.Headers.Add("Accept", "application/vnd.github.machine-man-preview+json");

                var planResponse = await HttpClient.SendAsync(planRequest);
                var planJson = await planResponse.Content.ReadAsStringAsync();

                Account[] accountsInPlan = new Account[0];
                try
                {
                    accountsInPlan = JsonConvert.DeserializeObject<Account[]>(planJson);
                }
                catch (Exception)
                {
                    logger.LogInformation("SerializationError:> " + planJson);
                }

                logger.LogInformation("MarketplaceSync found {NumPlans} for {PlanId}", accountsInPlan.Length, planId);

                // loop through the plans and get all purchases from github in order to sync with our database
                // ie. sync all purchases that are on git and not saved on our side
                Dictionary<int, bool> gitData = new Dictionary<int, bool>();
                foreach (var account in accountsInPlan)
                {
                    var row = new Common.TableModels.Marketplace(account.id, account.login)
                    {
                        AccountType = account.type,
                        PlanId = account.marketplace_purchase.plan.id
                    };
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(row));

                    // create a map to easily check the plans at the next step
                    gitData.Add(account.id, true);
                }

                // get all purchases for a plan from our database
                // then get all purchases for the plan from the github api
                // remove anything that is in our database and not on the github api response
                var query = new TableQuery<Common.TableModels.Marketplace>().Where(
                    "PlanId eq " + planId.ToString()).Take(1000);

                TableContinuationToken contToken = null;

                var deletedPurchases = new List<string>();
                do
                {
                    var rows = await marketplaceTable.ExecuteQuerySegmentedAsync(query, contToken);
                    contToken = rows.ContinuationToken;
                    if (!rows.Any())
                        continue;

                    bool redundantVariable;
                    foreach (var purchase in rows)
                    {
                        if (gitData.TryGetValue(purchase.AccountId, out redundantVariable))
                        {
                            continue;
                        }

                        deletedPurchases.Add(purchase.PartitionKey);

                        await marketplaceTable.DropRow(purchase.PartitionKey, purchase.AccountLogin);
                    }
                }
                while (contToken != null);

                logger.LogInformation("MarketplaceSync missing git purchases " + JsonConvert.SerializeObject(deletedPurchases));
                logger.LogInformation("MarketplaceSync finished");
            }
        }
    }
}