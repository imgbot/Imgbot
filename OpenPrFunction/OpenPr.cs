using System;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace OpenPrFunction
{
    public static class OpenPr
    {
        [Singleton("{InstallationId}")] // https://github.com/Azure/azure-webjobs-sdk/wiki/Singleton#scenarios
        [FunctionName("OpenPr")]
        public static async Task Trigger(
            [QueueTrigger("openprmessage")]OpenPrMessage openPrMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            [Table("pull")] ICollector<Pr> prs,
            ILogger logger,
            ExecutionContext context)
        {
            var storageAccount = CloudStorageAccount.Parse(KnownEnvironmentVariables.AzureWebJobsStorage);
            var settingsTable = storageAccount.CreateCloudTableClient().GetTableReference("settings");
            var installationTokenProvider = new InstallationTokenProvider();
            var pullRequest = new PullRequest();
            await RunAsync(openPrMessage, installation, prs, settingsTable, installationTokenProvider, pullRequest, logger, context)
                    .ConfigureAwait(false);
        }

        public static async Task RunAsync(
            OpenPrMessage openPrMessage,
            Installation installation,
            ICollector<Pr> prs,
            CloudTable settingsTable,
            IInstallationTokenProvider installationTokenProvider,
            IPullRequest pullRequest,
            ILogger logger,
            ExecutionContext context)
        {
            if (installation == null)
            {
                logger.LogError("No installation found for {InstallationId}", openPrMessage.InstallationId);
                throw new Exception($"No installation found for InstallationId: {openPrMessage.InstallationId}");
            }

            var installationToken = await installationTokenProvider.GenerateAsync(
                new InstallationTokenParameters
                {
                    AccessTokensUrl = string.Format(KnownGitHubs.AccessTokensUrlFormat, installation.InstallationId),
                    AppId = KnownGitHubs.AppId,
                },
                KnownEnvironmentVariables.APP_PRIVATE_KEY);

            logger.LogInformation("OpenPrFunction: Opening pull request for {Owner}/{RepoName}", installation.Owner, installation.RepoName);

            var settings = await SettingsHelper.GetSettings(settingsTable, installation.InstallationId, installation.RepoName);

            if (settings != null && !string.IsNullOrEmpty(settings.DefaultBranchOverride))
            {
                logger.LogInformation(
                    "OpenPrFunction: default branch override for {Owner}/{RepoName} is {DefaultBranchOverride}",
                    installation.Owner,
                    installation.RepoName,
                    settings.DefaultBranchOverride);
            }

            var result = await pullRequest.OpenAsync(
                new GitHubClientParameters
                {
                    Password = installationToken.Token,
                    RepoName = installation.RepoName,
                    RepoOwner = installation.Owner,
                },
                openPrMessage.Update,
                settings);

            if (result?.Id > 0)
            {
                logger.LogInformation("OpenPrFunction: Successfully opened pull request (#{PullRequestId}) for {Owner}/{RepoName}", result.Id, installation.Owner, installation.RepoName);
                prs.Add(result);
            }
        }
    }
}
