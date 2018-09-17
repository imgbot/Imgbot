using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Common.Messages;
using Common.TableModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table;
using NSubstitute;
using WebHook.Model;

namespace Test
{
    [TestClass]
    public class WebHookTests
    {
        [TestMethod]
        public async Task GivenCommitToOtherBranch_ShouldReturnOkDoNothing()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "push",
                payload: "data/hooks/commit-otherbranch.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("Commit to non default branch", response.Result);

            // No messages sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // No calls to MarketplaceTable
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenCommitToDefaultBranchNoImages_ShouldReturnOkDoNothing()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "push",
                payload: "data/hooks/commit-defaultbranch-noimages.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("No image files touched", response.Result);

            // No messages sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // No calls to MarketplaceTable
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenCommitToImgBotBranch_ShouldReturnOkQueueToOpenPr()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "push",
                payload: "data/hooks/commit-imgbotbranch.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("imgbot push", response.Result);

            // No messages sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // One message sent to OpenPr
            openPrMessages.Received(1).Add(Arg.Is<OpenPrMessage>(x =>
                                             x.InstallationId == 23199 &&
                                             x.RepoName == "test" &&
                                             x.CloneUrl == "https://github.com/dabutvin/test"));

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // No calls to MarketplaceTable
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenCommitToDefaultBranchWithImages_ShouldReturnOkQueueToRouter()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "push",
                payload: "data/hooks/commit-defaultbranch-images.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("truth", response.Result);

            // 1 message sent to Router
            routerMessages.Received(1).Add(Arg.Is<RouterMessage>(x =>
                x.InstallationId == 23199 &&
                x.Owner == "dabutvin" &&
                x.RepoName == "test" &&
                x.CloneUrl == "https://github.com/dabutvin/test"));

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // No calls to MarketplaceTable
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenNewInstallationAdded_ShouldReturnOkQueueRouter()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "installation_repositories",
                payload: "data/hooks/installation_repositories_added.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("truth", response.Result);

            // 1 message sent to Router
            routerMessages.Received(1).Add(Arg.Is<RouterMessage>(x =>
                          x.InstallationId == 554 &&
                          x.CloneUrl == "https://github.com/dabutvin/testing" &&
                          x.Owner == "dabutvin" &&
                         x.RepoName == "testing"));

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // No calls to MarketplaceTable to insert
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenNewInstallationCreated_ShouldReturnOkQueueRouter()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "integration_installation",
                payload: "data/hooks/installation_repositories_created.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("truth", response.Result);

            // 1 message sent to Router
            routerMessages.Received(1).Add(Arg.Is<RouterMessage>(x =>
                          x.InstallationId == 541 &&
                          x.CloneUrl == "https://github.com/dabutvin/myrepo" &&
                          x.Owner == "dabutvin" &&
                          x.RepoName == "myrepo"));

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // No calls to MarketplaceTable to insert
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenInstallationRemoved_ShouldReturnOkDropRow()
        {
            void ExtraSetup(
                ICollector<RouterMessage> extraRouterMessages,
                ICollector<OpenPrMessage> extraOpenPrMessages,
                CloudTable extraInstallationsTable,
                CloudTable extraMarketplaceTable) =>
            extraInstallationsTable
                .ExecuteAsync(Arg.Is<TableOperation>(x => x.OperationType == TableOperationType.Retrieve))
                .Returns(Task.FromResult(new TableResult
                {
                    Result = new Marketplace
                    {
                        ETag = "*",
                        RowKey = "1",
                        PartitionKey = "testing"
                    },
                }));

            var result = await ExecuteHookAsync(
                githubEvent: "integration_installation_repositories",
                payload: "data/hooks/installation_repositories_removed.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable,
                ExtraSetup);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("truth", response.Result);

            // 0 messages sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // 1 call to InstallationTable
            await installationsTable.Received(1).ExecuteAsync(Arg.Is<TableOperation>(x => x.OperationType == TableOperationType.Delete));

            // No calls to MarketplaceTable to insert
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenInstallationDeleted_ShouldReturnOkDropPartition()
        {
            var mockIntallations = new[]
            {
                new DynamicTableEntity("654321", "repo1") { ETag = "*" },
                new DynamicTableEntity("654321", "repo2") { ETag = "*" },
            }.ToList();

            var tableQuerySegment = (TableQuerySegment)typeof(TableQuerySegment)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .First(x => x.GetParameters().Count() == 1)
                .Invoke(new object[] { mockIntallations });

            void ExtraSetup(
                ICollector<RouterMessage> extraRouterMessages,
                ICollector<OpenPrMessage> extraOpenPrMessages,
                CloudTable extraInstallationsTable,
                CloudTable extraMarketplaceTable) =>
            extraInstallationsTable
                .ExecuteQuerySegmentedAsync(Arg.Any<TableQuery>(), Arg.Any<TableContinuationToken>())
                .Returns(Task.FromResult(tableQuerySegment));

            var result = await ExecuteHookAsync(
                githubEvent: "installation",
                payload: "data/hooks/installation_deleted.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable,
                ExtraSetup);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("truth", response.Result);

            // 0 message sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // 2 call to InstallationTable to delete
            await installationsTable.Received(2).ExecuteAsync(Arg.Is<TableOperation>(x => x.OperationType == TableOperationType.Delete));

            // No calls to MarketplaceTable to insert
            await marketplaceTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());
        }

        [TestMethod]
        public async Task GivenMarketplacePurchase_ShouldReturnOkWriteRow()
        {
            var result = await ExecuteHookAsync(
                githubEvent: "marketplace_purchase",
                payload: "data/hooks/marketplacepurchase.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("purchased", response.Result);

            // No messages sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // 1 call to MarketplaceTable to insert
            await marketplaceTable.Received(1).ExecuteAsync(
                Arg.Is<TableOperation>(x => x.OperationType == TableOperationType.InsertOrMerge));
        }

        [TestMethod]
        public async Task GivenMarketplaceCancellation_ShouldReturnOkDeleteRow()
        {
            void ExtraSetup(
                ICollector<RouterMessage> extraRouterMessages,
                ICollector<OpenPrMessage> extraOpenPrMessages,
                CloudTable extraInstallationsTable,
                CloudTable extraMarketplaceTable) =>
            extraMarketplaceTable
                .ExecuteAsync(Arg.Is<TableOperation>(x => x.OperationType == TableOperationType.Retrieve))
                .Returns(Task.FromResult(new TableResult
                {
                    Result = new Marketplace
                    {
                        ETag = "*",
                        RowKey = "1",
                        PartitionKey = "test"
                    },
                }));

            var result = await ExecuteHookAsync(
                githubEvent: "marketplace_purchase",
                payload: "data/hooks/marketplacecancellation.json",
                out var routerMessages,
                out var openPrMessages,
                out var installationsTable,
                out var marketplaceTable,
                ExtraSetup);

            // Assert OKObjectResult and Value
            var response = (HookResponse)((OkObjectResult)result).Value;
            Assert.AreEqual("cancelled", response.Result);

            // No messages sent to Router
            routerMessages.DidNotReceive().Add(Arg.Any<RouterMessage>());

            // No messages sent to OpenPr
            openPrMessages.DidNotReceive().Add(Arg.Any<OpenPrMessage>());

            // No calls to InstallationTable
            await installationsTable.DidNotReceive().ExecuteAsync(Arg.Any<TableOperation>());

            // 1 call to MarketplaceTable to delete
            await marketplaceTable.Received(1).ExecuteAsync(
                Arg.Is<TableOperation>(x => x.OperationType == TableOperationType.Delete));
        }

        private Task<IActionResult> ExecuteHookAsync(
            string githubEvent,
            string payload,
            out ICollector<RouterMessage> routerMessages,
            out ICollector<OpenPrMessage> openPrMessages,
            out CloudTable installationsTable,
            out CloudTable marketplaceTable,
            Action<ICollector<RouterMessage>, ICollector<OpenPrMessage>, CloudTable, CloudTable> extraSetup = null)
        {
            var request = Substitute.For<HttpRequestMessage>();
            routerMessages = Substitute.For<ICollector<RouterMessage>>();
            openPrMessages = Substitute.For<ICollector<OpenPrMessage>>();
            installationsTable = Substitute.For<CloudTable>(new Uri("https://myaccount.table.core.windows.net/Tables/installation"));
            marketplaceTable = Substitute.For<CloudTable>(new Uri("https://myaccount.table.core.windows.net/Tables/marketplace"));
            var logger = Substitute.For<ILogger>();

            request.Headers.Add("X-GitHub-Event", new[] { githubEvent });
            request.Content = new StringContent(File.ReadAllText(payload));

            extraSetup?.Invoke(routerMessages, openPrMessages, installationsTable, marketplaceTable);

            return WebHook.WebHookFunction.Run(
                request, routerMessages, openPrMessages, installationsTable, marketplaceTable, logger);
        }
    }
}
