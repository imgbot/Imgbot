using System;
using System.IO;
using System.Threading.Tasks;
using ImgBot.Common.Messages;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ImgBot.Function
{
    public static class Functions
    {
        [FunctionName("QueueTrigger")]
        public static async Task Run([QueueTrigger("installationmessage", Connection = "")]InstallationMessage installationMessage, TraceWriter log, ExecutionContext context)
        {
            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = installationMessage.AccessTokensUrl,
                AppId = 4706,
            };

            // good for ~10 minutes
            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText("imgbot.2017-08-23.private-key.pem"));

            var localPath = Path.Combine(context.FunctionDirectory, installationMessage.RepoName + new Random().Next(100, 99999).ToString());

            var compressImagesParameters = new CompressimagesParameters
            {
                CloneUrl = installationMessage.CloneUrl,
                LocalPath = localPath,
                Username = "x-access-token",
                Password = installationToken.token,
                RepoName = installationMessage.RepoName,
                RepoOwner = installationMessage.Owner,
            };

            await CompressImages.RunAsync(compressImagesParameters);
        }
    }
}
