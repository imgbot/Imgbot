using System;
using System.IO;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CompressImagesFunction
{
    public static class CompressImagesFunction
    {
        [Singleton("{RepoName}")] // https://github.com/Azure/azure-webjobs-sdk/wiki/Singleton#scenarios
        [FunctionName("CompressImagesFunction")]
        public static Task Trigger(
            [QueueTrigger("compressimagesmessage")]CompressImagesMessage compressImagesMessage,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            ILogger logger,
            ExecutionContext context)
        {
            var installationTokenProvider = new InstallationTokenProvider();
            return RunAsync(installationTokenProvider, compressImagesMessage, openPrMessages, logger, context);
        }

        public static async Task RunAsync(
            IInstallationTokenProvider installationTokenProvider,
            CompressImagesMessage compressImagesMessage,
            ICollector<OpenPrMessage> openPrMessages,
            ILogger logger,
            ExecutionContext context)
        {
            logger.LogInformation("CompressImagesFunction: starting run for {Owner}/{RepoName}", compressImagesMessage.Owner, compressImagesMessage.RepoName);
            var installationTokenParameters = new InstallationTokenParameters
            {
                AccessTokensUrl = string.Format(KnownGitHubs.AccessTokensUrlFormat, compressImagesMessage.InstallationId),
                AppId = KnownGitHubs.AppId,
            };

            var installationToken = await installationTokenProvider.GenerateAsync(
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

            var didCompress = CompressImages.Run(compressImagesParameters, logger);

            if (didCompress)
            {
                logger.LogInformation("CompressImagesFunction: Successfully compressed images for {Owner}/{RepoName}", compressImagesMessage.Owner, compressImagesMessage.RepoName);
                openPrMessages.Add(new OpenPrMessage
                {
                    InstallationId = compressImagesMessage.InstallationId,
                    RepoName = compressImagesMessage.RepoName,
                    CloneUrl = compressImagesMessage.CloneUrl,
                });
            }

            logger.LogInformation("CompressImagesFunction: finished run for {Owner}/{RepoName}", compressImagesMessage.Owner, compressImagesMessage.RepoName);
        }
    }
}
