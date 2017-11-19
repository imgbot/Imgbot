using System;
using System.IO;
using System.Threading.Tasks;
using ImgBot.Common;
using ImgBot.Common.Messages;
using ImgBot.Common.TableModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ImgBot.Function
{
    public static class Functions
    {
        private static Random _random = new Random();

        [FunctionName("imageupdatemessage")]
        public static async Task RunImageUpdateMessage(
            [QueueTrigger("imageupdatemessage")]ImageUpdateMessage imageUpdateMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            TraceWriter log,
            ExecutionContext context)
        {
            if (installation == null)
            {
                throw new Exception($"No installation found for InstallationId: {installation.InstallationId}");
            }

            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = installation.AccessTokensUrl,
                AppId = KnownGitHubs.AppId,
            };

            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText(Path.Combine(context.FunctionDirectory, $"..\\{KnownGitHubs.PrivateKeyFilename}")));

            var compressImagesParameters = new CompressimagesParameters
            {
                CloneUrl = installation.CloneUrl,
                LocalPath = LocalPath.CloneDir(Environment.GetEnvironmentVariable("TMP"), installation.RepoName),
                Password = installationToken.Token,
                RepoName = installation.RepoName,
                RepoOwner = installation.Owner,
            };

            var didCompress = CompressImages.Run(compressImagesParameters);

            if (didCompress)
            {
                openPrMessages.Add(new OpenPrMessage
                {
                    InstallationId = imageUpdateMessage.InstallationId,
                    RepoName = imageUpdateMessage.RepoName,
                });
            }
        }

        [FunctionName("installationmessage")]
        public static async Task RunInstallationMessage(
            [QueueTrigger("installationmessage")]InstallationMessage installationMessage,
            [Table("installation")] ICollector<Installation> installations,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            TraceWriter log,
            ExecutionContext context)
        {
            // if not already installed
            if (installation == null)
            {
                installations.Add(new Installation(installationMessage.InstallationId, installationMessage.RepoName)
                {
                    AccessTokensUrl = installationMessage.AccessTokensUrl,
                    CloneUrl = installationMessage.CloneUrl,
                    Owner = installationMessage.Owner,
                });
            }

            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = installationMessage.AccessTokensUrl,
                AppId = KnownGitHubs.AppId,
            };

            // good for ~10 minutes
            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText(Path.Combine(context.FunctionDirectory, $"..\\{KnownGitHubs.PrivateKeyFilename}")));

            var compressImagesParameters = new CompressimagesParameters
            {
                CloneUrl = installationMessage.CloneUrl,
                LocalPath = LocalPath.CloneDir(Environment.GetEnvironmentVariable("TMP"), installationMessage.RepoName),
                Password = installationToken.Token,
                RepoName = installationMessage.RepoName,
                RepoOwner = installationMessage.Owner,
            };

            var didCompress = CompressImages.Run(compressImagesParameters);

            if (didCompress)
            {
                openPrMessages.Add(new OpenPrMessage
                {
                    InstallationId = installationMessage.InstallationId,
                    RepoName = installationMessage.RepoName,
                });
            }
        }

        [Singleton("{InstallationId}")] // https://github.com/Azure/azure-webjobs-sdk/wiki/Singleton#scenarios
        [FunctionName("openprmessage")]
        public static async Task RunOpenPr(
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
                AccessTokensUrl = installation.AccessTokensUrl,
                AppId = KnownGitHubs.AppId,
            };

            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText(Path.Combine(context.FunctionDirectory, $"..\\{KnownGitHubs.PrivateKeyFilename}")));

            await PullRequest.OpenAsync(new PullRequestParameters
            {
                Password = installationToken.Token,
                RepoName = installation.RepoName,
                RepoOwner = installation.Owner,
            });
        }
    }
}
