using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Octokit.Internal;

namespace DeleteBranchFunction
{
    public static class DeleteBranch
    {
        [Singleton("{InstallationId}")] // https://github.com/Azure/azure-webjobs-sdk/wiki/Singleton#scenarios
        [FunctionName("DeleteBranch")]
        public static async Task Trigger(
            [QueueTrigger("deletebranchmessage")]DeleteBranchMessage deleteBranchMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            ILogger logger,
            ExecutionContext context)
        {
            var installationTokenProvider = new InstallationTokenProvider();
            await RunAsync(deleteBranchMessage, installation, installationTokenProvider, logger, context).ConfigureAwait(false);
        }

        public static async Task RunAsync(
            DeleteBranchMessage deleteBranchMessage,
            Installation installation,
            IInstallationTokenProvider installationTokenProvider,
            ILogger logger,
            ExecutionContext context)
        {
            if (installation == null)
            {
                logger.LogError("No installation found for {InstallationId}", deleteBranchMessage.InstallationId);
                throw new Exception($"No installation found for InstallationId: {deleteBranchMessage.InstallationId}");
            }

            var installationToken = await installationTokenProvider.GenerateAsync(
                new InstallationTokenParameters
                {
                    AccessTokensUrl = string.Format(KnownGitHubs.AccessTokensUrlFormat, installation.InstallationId),
                    AppId = KnownGitHubs.AppId,
                },
                KnownEnvironmentVariables.APP_PRIVATE_KEY);

            logger.LogInformation("DeleteBranchFunction: Deleting imgbot branch for {Owner}/{RepoName}", installation.Owner, installation.RepoName);

            var inMemoryCredentialStore = new InMemoryCredentialStore(new Octokit.Credentials(KnownGitHubs.Username, installationToken.Token));
            var githubClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("ImgBot"), inMemoryCredentialStore);

            var imgbotRefName = $"heads/{KnownGitHubs.BranchName}";
            Octokit.Reference imgbotBranchRef = null;
            try
            {
                imgbotBranchRef = await githubClient.Git.Reference.Get(installation.Owner, installation.RepoName, imgbotRefName);
            }
            catch
            {
            }

            if (imgbotBranchRef == null)
            {
                logger.LogInformation("DeleteBranchFunction: No imgbot branch found for {Owner}/{RepoName}", installation.Owner, installation.RepoName);
                return;
            }

            await githubClient.Git.Reference.Delete(installation.Owner, installation.RepoName, imgbotRefName);
            logger.LogInformation("DeleteBranchFunction: Successfully deleted imgbot branch for {Owner}/{RepoName}", installation.Owner, installation.RepoName);
        }
    }
}
