using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace OpenPrFunction
{
    public static class OpenPr
    {
        [Singleton("{InstallationId}")] // https://github.com/Azure/azure-webjobs-sdk/wiki/Singleton#scenarios
        [FunctionName("OpenPr")]
        public static async Task Run(
            [QueueTrigger("openprmessage")]OpenPrMessage openPrMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            TraceWriter log,
            ExecutionContext context)
        {
            if (installation == null)
            {
                throw new Exception($"No installation found for InstallationId: {installation.InstallationId}");
            }

            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = string.Format(KnownGitHubs.AccessTokensUrlFormat, installation.InstallationId),
                AppId = KnownGitHubs.AppId,
            };

            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText(Path.Combine(context.FunctionDirectory, $"../{KnownGitHubs.AppPrivateKey}")));

            await PullRequest.OpenAsync(new PullRequestParameters
            {
                Password = installationToken.Token,
                RepoName = installation.RepoName,
                RepoOwner = installation.Owner,
            });
        }
    }
}
