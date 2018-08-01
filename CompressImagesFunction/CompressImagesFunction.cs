using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace CompressImagesFunction
{
    public static class CompressImagesFunction
    {
        [FunctionName("CompressImagesFunction")]
        public static async Task Run(
            [QueueTrigger("compressimagesmessage")]CompressImagesMessage compressImagesMessage,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            TraceWriter log,
            ExecutionContext context)
        {
            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = compressImagesMessage.AccessTokensUrl,
                AppId = KnownGitHubs.AppId,
            };

            var installationToken = await InstallationToken.GenerateAsync(
                installationTokenParameters,
                File.OpenText(Path.Combine(context.FunctionDirectory, $"../{KnownGitHubs.AppPrivateKey}")));

            var compressImagesParameters = new CompressimagesParameters
            {
                CloneUrl = compressImagesMessage.CloneUrl,
                LocalPath = LocalPath.CloneDir(Environment.GetEnvironmentVariable("TMP") ?? "/private/tmp/", compressImagesMessage.RepoName),
                Password = installationToken.Token,
                RepoName = compressImagesMessage.RepoName,
                RepoOwner = compressImagesMessage.Owner,
                PgpPrivateKeyStream = File.OpenRead(Path.Combine(context.FunctionDirectory, $"../{KnownGitHubs.PGPPrivateKeyFilename}")),
                PgPPassword = File.ReadAllText(Path.Combine(context.FunctionDirectory, $"../{KnownGitHubs.PGPPasswordFilename}"))
            };

            var didCompress = CompressImages.Run(compressImagesParameters);

            if (didCompress)
            {
                log.Info("Compressed images; Route to OpenPR");
                openPrMessages.Add(new OpenPrMessage
                {
                    InstallationId = compressImagesMessage.InstallationId,
                    RepoName = compressImagesMessage.RepoName,
                    CloneUrl = compressImagesMessage.CloneUrl,
                });
            }

            log.Info("Completed run");
        }
    }
}
