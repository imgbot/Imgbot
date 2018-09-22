using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace OpenPrFunction
{
    public static class OpenPr
    {
        [Singleton("{InstallationId}")] // https://github.com/Azure/azure-webjobs-sdk/wiki/Singleton#scenarios
        [FunctionName("OpenPr")]
        public static async Task Run(
            [QueueTrigger("openprmessage")]OpenPrMessage openPrMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            ILogger logger,
            ExecutionContext context)
        {
            if (installation == null)
            {
                logger.LogError("No installation found for {InstallationId}", openPrMessage.InstallationId);
                throw new Exception($"No installation found for InstallationId: {openPrMessage.InstallationId}");
            }

            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = string.Format(KnownGitHubs.AccessTokensUrlFormat, installation.InstallationId),
                AppId = KnownGitHubs.AppId,
            };

            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText(Path.Combine(context.FunctionDirectory, $"../{KnownGitHubs.AppPrivateKey}")));

            logger.LogInformation("OpenPrFunction: Opening pull request for {Owner}/{RepoName}", installation.Owner, installation.RepoName);
            var id = await PullRequest.OpenAsync(new PullRequestParameters
            {
                Password = installationToken.Token,
                RepoName = installation.RepoName,
                RepoOwner = installation.Owner,
            });

            if (id > 0)
            {
                logger.LogInformation("OpenPrFunction: Successfully opened pull request (#{PullRequestId}) for {Owner}/{RepoName}", id, installation.Owner, installation.RepoName);
            }
        }
    }
}
