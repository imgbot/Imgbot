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
        private static Random s_random = new Random();

        [FunctionName("imageupdatemessage")]
        public static async Task RunImageUpdateMessage(
            [QueueTrigger("imageupdatemessage")]ImageUpdateMessage imageUpdateMessage,
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

            var compressImagesParameters = new CompressimagesParameters
            {
                CloneUrl = installation.CloneUrl,
                LocalPath = LocalPath.CloneDir(Environment.GetEnvironmentVariable("TMP"), installation.RepoName),
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

            if (installation == null) // not already installed
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
                Password = installationToken.token,
                RepoName = installationMessage.RepoName,
                RepoOwner = installationMessage.Owner,
            };

            // getting duplicate work happening at the same exact time in multiple threads
            // the most common case is install and add events on the same repo when the queue is asleep
            // the queue wakes up and dequeues both messages at the same time in 2 threads
            // wait a random amount of seconds so that one will win
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(s_random.Next(0, 20)));

            await CompressImages.RunAsync(compressImagesParameters);
        }
    }
}
