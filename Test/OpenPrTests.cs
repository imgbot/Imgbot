using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Common.TableModels;
using Install;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;
using NSubstitute;
using OpenPrFunction;

namespace Test
{
    [TestClass]
    public class OpenPrTests
    {
        [TestMethod]
        public async Task ShouldOpenPrAndLog()
        {
            await ExecuteRunAsync(12345, "dabutvin", "test", 150, out var logger).ConfigureAwait(false);

            logger.AssertCallCount(2);

            logger.FirstCall().AssertLogLevel(LogLevel.Information);
            logger.FirstCall().AssertLogMessage("OpenPrFunction: Opening pull request for {Owner}/{RepoName}");
            logger.FirstCall().AssertLogValues(
                KeyValuePair.Create("Owner", "dabutvin"),
                KeyValuePair.Create("RepoName", "test"));

            logger.SecondCall().AssertLogLevel(LogLevel.Information);
            logger.SecondCall().AssertLogMessage("OpenPrFunction: Successfully opened pull request (#{PullRequestId}) for {Owner}/{RepoName}");
            logger.SecondCall().AssertLogValues(
                KeyValuePair.Create("PullRequestId", "150"),
                KeyValuePair.Create("Owner", "dabutvin"),
                KeyValuePair.Create("RepoName", "test"));
        }

        [TestMethod]
        public async Task ShouldNotLog_GivenPrIdUnder1()
        {
            await ExecuteRunAsync(12345, "dabutvin", "test", 0, out var logger).ConfigureAwait(false);

            logger.AssertCallCount(1);

            logger.FirstCall().AssertLogLevel(LogLevel.Information);
            logger.FirstCall().AssertLogMessage("OpenPrFunction: Opening pull request for {Owner}/{RepoName}");
            logger.FirstCall().AssertLogValues(
                KeyValuePair.Create("Owner", "dabutvin"),
                KeyValuePair.Create("RepoName", "test"));
        }

        [TestMethod]
        public async Task ShouldThrow_GivenMissingInstallation()
        {
            ILogger logger = null;
            try
            {
                await ExecuteRunAsync(
                    new OpenPrMessage
                    {
                        CloneUrl = "https://github.com/dabutvin/test",
                        InstallationId = 1234,
                        RepoName = "test",
                    },
                    null,
                    1,
                    out logger);
            }
            catch (Exception e)
            {
                Assert.AreEqual("No installation found for InstallationId: 1234", e.Message);
                logger.AssertCallCount(1);
                logger.FirstCall().AssertLogLevel(LogLevel.Error);
                logger.FirstCall().AssertLogMessage("No installation found for {InstallationId}");
                logger.FirstCall().AssertLogValues(KeyValuePair.Create("InstallationId", "1234"));
            }
        }

        private Task ExecuteRunAsync(int installationId, string owner, string repoName, long prId, out ILogger logger)
        {
            var cloneUrl = $"https://github.com/{owner}/{repoName}";

            var openPrMessage = new OpenPrMessage
            {
                CloneUrl = cloneUrl,
                InstallationId = installationId,
                RepoName = repoName
            };

            var installation = new Installation
            {
                CloneUrl = cloneUrl,
                InstallationId = installationId,
                RepoName = repoName,
                PartitionKey = installationId.ToString(),
                RowKey = repoName,
                Owner = owner
            };

            return ExecuteRunAsync(openPrMessage, installation, prId, out logger);
        }

        private Task ExecuteRunAsync(OpenPrMessage openPrMessage, Installation installation, long prId, out ILogger logger)
        {
            logger = Substitute.For<ILogger>();

            var context = Substitute.For<ExecutionContext>();
            context.FunctionDirectory = "data/functiondir";

            var installationTokenProvider = Substitute.For<IInstallationTokenProvider>();
            installationTokenProvider
                 .GenerateAsync(Arg.Any<InstallationTokenParameters>(), Arg.Any<string>())
                 .Returns(Task.FromResult(new InstallationToken
                 {
                     Token = "token",
                     ExpiresAt = "12345"
                 }));

            var pullRequest = Substitute.For<IPullRequest>();
            pullRequest.OpenAsync(Arg.Any<GitHubClientParameters>(), false).Returns(x => Task.FromResult(new Pr(installation.Owner) { Id = prId }));

            var settingsTable = Substitute.For<CloudTable>(new Uri("https://myaccount.table.core.windows.net/Tables/settings"));

            var prs = Substitute.For<ICollector<Pr>>();

            return OpenPr.RunAsync(openPrMessage, installation, prs, settingsTable, installationTokenProvider, pullRequest, logger, context);
        }
    }
}
