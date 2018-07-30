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

namespace CompressPoisonHandler
{
    /// <summary>
    /// The point of this Timer function is to clean up after the main function
    /// It is read only as it relates to GitHub - only querying for status
    /// The action it takes is moving queue messages around
    /// If there is work to do it moves the message back into the main compress images queue
    /// If there is no work to do it deletes the message entirely
    /// </summary>
    public static class CompressPoisonFunction
    {
        [Singleton]
        [FunctionName("CompressPoison")]
        public static async Task Run(
            [TimerTrigger("0 */30 * * * *", RunOnStartup = true)]TimerInfo timerInfo,
            [Table("installation")] CloudTable installationTable,
            [Queue("compressimagesmessage")] CloudQueue compressImagesQueue,
            [Queue("compressimagesmessage-poison")] CloudQueue compressImagesPoisonQueue,
            TraceWriter log,
            ExecutionContext context)
        {
            for (var i = 0; i < 50; i++)
            {
                System.Threading.Thread.Sleep(1000);

                var topQueueMessage = await compressImagesPoisonQueue.GetMessageAsync();
                if (topQueueMessage == null)
                {
                    continue;
                }

                // pre-emptively delete the message from the queue we are pulling from
                await compressImagesPoisonQueue.DeleteMessageAsync(topQueueMessage);
                var topMessage = JsonConvert.DeserializeObject<CompressImagesMessage>(topQueueMessage.AsString);

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

                    var appClient = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("CompressPoison"))
                    {
                        Credentials = new Octokit.Credentials(installationToken.Token, Octokit.AuthenticationType.Bearer)
                    };

                    var limits = await appClient.Miscellaneous.GetRateLimits();

                    log.Info("Ratelimits:\n");
                    log.Info(JsonConvert.SerializeObject(limits));

                    // check if an 'imgbot' branch is open
                    var branches = await appClient.Repository.Branch.GetAll(installation.Owner, installation.RepoName);
                    var imgbotBranches = branches.Where(x => x.Name == "imgbot");

                    if (imgbotBranches.Count() == 1)
                    {
                        // we have open imgbot branches right now, let's just leave
                        continue;
                    }
                    else
                    {
                        log.Info("Open 'imgbot' branch not found");
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
                        log.Info("no imgbot prs in history, let's queue a message to try and compress images");
                        await compressImagesQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(topMessage)));
                    }
                }
                catch (Exception e)
                {
                    log.Error("ERROR!", e);

                    // add it back to the poison queue
                    await compressImagesPoisonQueue.AddMessageAsync(topQueueMessage);
                }
            }
        }
    }
}
