using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using CompressImagesFunction.Compress;
using CompressImagesFunction.Repo;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace CompressImagesFunction
{
    public static class CompressImagesFunction
    {
        [FunctionName("CompressImagesFunction")]
        public static async Task Trigger(
            [QueueTrigger("compressimagesmessage")]CompressImagesMessage compressImagesMessage,
            [Queue("longrunningcompressmessage")] ICollector<CompressImagesMessage> longRunningCompressMessages,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            ILogger logger,
            ExecutionContext context)
        {
            logger.LogInformation($"Starting compress");
            var installationTokenProvider = new InstallationTokenProvider();
            var repoChecks = new RepoChecks();
            var task = RunAsync(installationTokenProvider, compressImagesMessage, longRunningCompressMessages, openPrMessages, repoChecks, logger, context);
            if (await Task.WhenAny(task, Task.Delay(570000)) == task)
            {
                await task;
            }
            else
            {
                logger.LogInformation($"Time out exceeded!");
                longRunningCompressMessages.Add(compressImagesMessage);
            }
        }

        [FunctionName("LongCompressImagesFunction")]
        public static async Task LongTrigger(
            [QueueTrigger("longrunningcompressmessage")]CompressImagesMessage compressImagesMessage,
            [Queue("longrunningcompressmessage")] ICollector<CompressImagesMessage> longRunningCompressMessages,
            [Queue("openprmessage")] ICollector<OpenPrMessage> openPrMessages,
            ILogger logger,
            ExecutionContext context)
        {
            logger.LogInformation($"Starting long compress");
            var installationTokenProvider = new InstallationTokenProvider();
            var repoChecks = new RepoChecks();
            var task = RunAsync(installationTokenProvider, compressImagesMessage, longRunningCompressMessages, openPrMessages, repoChecks, logger, context);
            await task;
        }

        public static async Task RunAsync(
            IInstallationTokenProvider installationTokenProvider,
            CompressImagesMessage compressImagesMessage,
            ICollector<CompressImagesMessage> nextPageMessages,
            ICollector<OpenPrMessage> openPrMessages,
            IRepoChecks repoChecks,
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
                KnownEnvironmentVariables.APP_PRIVATE_KEY);

            // check if repo is archived before starting work
            var isArchived = await repoChecks.IsArchived(new GitHubClientParameters
            {
                Password = installationToken.Token,
                RepoName = compressImagesMessage.RepoName,
                RepoOwner = compressImagesMessage.Owner
            });

            if (isArchived)
            {
                logger.LogInformation("CompressImagesFunction: skipping archived repo {Owner}/{RepoName}", compressImagesMessage.Owner, compressImagesMessage.RepoName);
                return;
            }

            // check if imgbot branch already exists before starting work
            var branchExists = await repoChecks.BranchExists(new GitHubClientParameters
            {
                Password = installationToken.Token,
                RepoName = compressImagesMessage.RepoName,
                RepoOwner = compressImagesMessage.Owner,
            });

            if (branchExists)
            {
                logger.LogInformation("CompressImagesFunction: skipping repo {Owner}/{RepoName} as branch exists", compressImagesMessage.Owner, compressImagesMessage.RepoName);
                return;
            }

            var compressImagesParameters = new CompressimagesParameters
            {
                CloneUrl = compressImagesMessage.CloneUrl,
                LocalPath = LocalPath.CloneDir(KnownEnvironmentVariables.TMP ?? "/private/tmp/", compressImagesMessage.RepoName),
                Password = installationToken.Token,
                RepoName = compressImagesMessage.RepoName,
                RepoOwner = compressImagesMessage.Owner,
                PgpPrivateKey = KnownEnvironmentVariables.PGP_PRIVATE_KEY,
                PgPPassword = KnownEnvironmentVariables.PGP_PASSWORD,
                CompressImagesMessage = compressImagesMessage,
                UpdatedImages = compressImagesMessage.UpdatedImages,
            };

            if (compressImagesMessage.Page.HasValue)
            {
                compressImagesParameters.Page = compressImagesMessage.Page.Value;
            }

            var compressionRunResult = CompressImages.Run(compressImagesParameters, logger);

            if (compressionRunResult.DidCompress)
            {
                logger.LogInformation("CompressImagesFunction: Successfully compressed images for {Owner}/{RepoName}", compressImagesMessage.Owner, compressImagesMessage.RepoName);
                openPrMessages.Add(new OpenPrMessage
                {
                    InstallationId = compressImagesMessage.InstallationId,
                    RepoName = compressImagesMessage.RepoName,
                    CloneUrl = compressImagesMessage.CloneUrl,
                });
            }
            else if (compressionRunResult.RunNextPage)
            {
                logger.LogInformation("CompressImagesFunction: Move to page: {Page} for {Owner}/{RepoName}", compressImagesParameters.Page + 1, compressImagesMessage.Owner, compressImagesMessage.RepoName);
                nextPageMessages.Add(new CompressImagesMessage
                {
                    CloneUrl = compressImagesMessage.CloneUrl,
                    InstallationId = compressImagesMessage.InstallationId,
                    Owner = compressImagesMessage.Owner,
                    RepoName = compressImagesMessage.Owner,
                    Page = compressImagesParameters.Page + 1,
                });
            }

            logger.LogInformation("CompressImagesFunction: finished run for {Owner}/{RepoName}", compressImagesMessage.Owner, compressImagesMessage.RepoName);
        }
    }
}
