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
        [FunctionName("imageupdatemessage")]
        public static async Task RunImageUpdateMessage(
            [QueueTrigger("imageupdatemessage")]ImageUpdateMessage imageUpdateMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            TraceWriter log,
            ExecutionContext context)
        {
            if (installation == null)
            {
                throw new Exception($"No installation found for installion: {installation.InstallationId}");
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
                LocalPath = LocalPath.CloneDir(context.FunctionDirectory, installation.RepoName),
                Password = installationToken.token,
                RepoName = installation.RepoName,
                RepoOwner = installation.Owner,
            };

            await CompressImages.RunAsync(compressImagesParameters);
        }

        [FunctionName("installationmessage")]
        public static async Task RunInstallationMessage(
            [QueueTrigger("installationmessage")]InstallationMessage installationMessage,
            [Table("installation")] ICollector<Installation> installations,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            TraceWriter log,
            ExecutionContext context)
        {

            if (installation != null) // already installed
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
                LocalPath = LocalPath.CloneDir(context.FunctionDirectory, installationMessage.RepoName),
                Password = installationToken.token,
                RepoName = installationMessage.RepoName,
                RepoOwner = installationMessage.Owner,
            };

            await CompressImages.RunAsync(compressImagesParameters);
        }
    }
}
