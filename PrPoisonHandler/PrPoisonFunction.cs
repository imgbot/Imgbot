using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace OpenPrFunction
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
        public static async Task Run(
            [TimerTrigger("0 */30 * * * *", RunOnStartup = true)]TimerInfo timerInfo,
            [Table("installation")] CloudTable installationTable,
            [Queue("openprmessage")] CloudQueue openPrQueue,
            [Queue("openprmessage-poison")] CloudQueue openPrPoisonQueue,
            TraceWriter log,
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
                        log.Info("Not listed in installation table");
                        continue;
                    }

                    log.Info($"https://github.com/{installation.Owner}/{installation.RepoName}");

                    var installationTokenParameters = new InstallationTokenParameters
                    {
                        AccessTokensUrl = installation.AccessTokensUrl,
                        AppId = KnownGitHubs.AppId,
                    };

                    var installationToken = await InstallationToken.GenerateAsync(
                        installationTokenParameters,
                        File.OpenText(Path.Combine(context.FunctionDirectory, $"../{KnownGitHubs.AppPrivateKey}")));

                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("MyApp"))
                    {
                        Credentials = new Octokit.Credentials(installationToken.Token, Octokit.AuthenticationType.Bearer)
                    };

                    var limits = await appClient.Miscellaneous.GetRateLimits();

                    log.Info("Ratelimits:\n");
                    log.Info(JsonConvert.SerializeObject(limits));

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
                        log.Info("Open 'imgbot' branch found");
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
                        log.Info("Open 'imgbot' PR not found, do we need to open one?");
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
                    log.Error("ERROR!", e);

                    // add it back to the poison queue
                    await openPrPoisonQueue.AddMessageAsync(topQueueMessage);
                }
            }
        }
    }
}
