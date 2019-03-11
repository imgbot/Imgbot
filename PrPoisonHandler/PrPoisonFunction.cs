using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace PrPoisonHandler
{
    /// <summary>
    /// The point of this Timer function is to clean up after the main function
    /// It is read only as it relates to GitHub - only querying for status
    /// The action it takes is moving queue messages around
    /// If there is work to do it moves the message back into the main openPR queue
    /// If there is no work to do it deletes the message entirely
    /// </summary>
    public static class PrPoisonFunction
    {
        [Singleton]
        [FunctionName("PrPoison")]
        public static Task Trigger(
            [TimerTrigger("0 */30 * * * *", RunOnStartup = true)]TimerInfo timerInfo,
            ILogger logger,
            ExecutionContext context)
        {
            var storageAccount = CloudStorageAccount.Parse(KnownEnvironmentVariables.AzureWebJobsStorage);
            var installationTable = storageAccount.CreateCloudTableClient().GetTableReference("installation");
            var openPrQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("openprmessage");
            var openPrPoisonQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("openprmessage-poison");
            var installationTokenProvider = new InstallationTokenProvider();

            return RunAsync(installationTokenProvider, installationTable, openPrQueue, openPrPoisonQueue, logger, context);
        }

        public static async Task RunAsync(
            IInstallationTokenProvider installationTokenProvider,
            CloudTable installationTable,
            CloudQueue openPrQueue,
            CloudQueue openPrPoisonQueue,
            ILogger logger,
            ExecutionContext context)
        {
            for (var i = 0; i < 100; i++)
            {
                System.Threading.Thread.Sleep(1000);

                var topQueueMessage = await openPrPoisonQueue.GetMessageAsync();
                if (topQueueMessage == null)
                {
                    continue;
                }

                // pre-emptively delete the message from the queue we are pulling from
                await openPrPoisonQueue.DeleteMessageAsync(topQueueMessage);
                var topMessage = JsonConvert.DeserializeObject<OpenPrMessage>(topQueueMessage.AsString);

                try
                {
                    var installation = (Installation)(await installationTable.ExecuteAsync(
                        TableOperation.Retrieve<Installation>(topMessage.InstallationId.ToString(), topMessage.RepoName)))
                        .Result;

                    if (installation == null)
                    {
                        logger.LogInformation("Not listed in installation table");
                        continue;
                    }

                    logger.LogInformation($"https://github.com/{installation.Owner}/{installation.RepoName}");

                    var installationTokenParameters = new InstallationTokenParameters
                    {
                        AccessTokensUrl = string.Format(KnownGitHubs.AccessTokensUrlFormat, topMessage.InstallationId),
                        AppId = KnownGitHubs.AppId,
                    };

                    var installationToken = await installationTokenProvider.GenerateAsync(
                        installationTokenParameters,
                        KnownEnvironmentVariables.APP_PRIVATE_KEY);

                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("MyApp"))
                    {
                        Credentials = new Octokit.Credentials(installationToken.Token, Octokit.AuthenticationType.Bearer)
                    };

                    var limits = await appClient.Miscellaneous.GetRateLimits();

                    logger.LogInformation("Ratelimits:\n");
                    logger.LogInformation(JsonConvert.SerializeObject(limits));

                    // check if an 'imgbot' branch is open
                    var branches = await appClient.Repository.Branch.GetAll(installation.Owner, installation.RepoName);
                    var imgbotBranches = branches.Where(x => x.Name == "imgbot");

                    if (imgbotBranches.Count() == 0)
                    {
                        // we have no open imgbot branches right now, let's just leave
                        continue;
                    }
                    else
                    {
                        logger.LogInformation("Open 'imgbot' branch found");
                    }

                    // check for ImgBot PRs
                    var prs = await appClient.Repository.PullRequest.GetAllForRepository(installation.Owner, installation.RepoName);
                    var imgbotPrs = prs.Where(x => x.Head.Ref == "imgbot");

                    if (imgbotPrs.Count() > 0)
                    {
                        // we have an open imgbot PR right now, let's just leave
                        continue;
                    }
                    else
                    {
                        logger.LogInformation("Open 'imgbot' PR not found, do we need to open one?");
                    }

                    // query for closed ImgBot PRs
                    var searchRequest = new Octokit.SearchIssuesRequest("imgbot")
                    {
                        Type = Octokit.IssueTypeQualifier.PullRequest,
                        Repos = new Octokit.RepositoryCollection { installation.Owner + "/" + installation.RepoName }
                    };

                    var imgbotIssues = await appClient.Search.SearchIssues(searchRequest);
                    if (imgbotIssues.TotalCount == 0)
                    {
                        // no imgbot prs in history, let's queue a message to get the pr open
                        await openPrQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(topMessage)));
                    }
                    else
                    {
                        // this is the case where an 'imgbot' branch exists, but there are closed imgbot prs
                        var latestClosedPr = imgbotIssues.Items.OrderByDescending(x => x.ClosedAt).First();
                        var potentialBranch = branches.First(x => x.Name == "imgbot");

                        var commitInImgbotBranch = await appClient.Repository.Commit
                                                            .Get(installation.Owner, installation.RepoName, potentialBranch.Commit.Sha);

                        if (commitInImgbotBranch.Commit.Author.Date > latestClosedPr.ClosedAt)
                        {
                            // if the branch is newer than the last closed imgbot PR then we should queue a message to get the pr open
                            await openPrQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(topMessage)));
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "ERROR!");

                    // add it back to the poison queue
                    await openPrPoisonQueue.AddMessageAsync(topQueueMessage);
                }
            }
        }
    }
}
