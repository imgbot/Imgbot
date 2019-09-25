using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common;
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

            foreach (var planId in new[] { 2840, 2841 })
            {
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

                foreach (var account in accountsInPlan)
                {
                    var row = new Common.TableModels.Marketplace(account.id, account.login)
                    {
                        AccountType = account.type,
                        PlanId = account.marketplace_purchase.plan.id
                    };
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(row));
                }
            }

            logger.LogInformation("MarketplaceSync finished");
        }
    }
}
