using System;
using Common;
using CompressImagesFunction;
using LibGit2Sharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Test
{
    [TestClass]
    public class ScheduleTests
    {
        private readonly RepoConfiguration _dailyConfiguration = new RepoConfiguration
        {
            Schedule = KnownScheduleSettings.Daily,
        };

        private readonly RepoConfiguration _weeklyConfiguration = new RepoConfiguration
        {
            Schedule = KnownScheduleSettings.Weekly,
        };

        private readonly RepoConfiguration _monthlyConfiguration = new RepoConfiguration
        {
            Schedule = KnownScheduleSettings.Monthly,
        };

        [TestMethod]
        public void GivenDefaultConfiguration_ShouldOptimizeImages()
        {
            var repository = Substitute.For<IRepository>();
            var shouldOptimize = Schedule.ShouldOptimizeImages(new RepoConfiguration(), repository);
            Assert.IsTrue(shouldOptimize);
        }

        [TestMethod]
        public void GivenImgBotNeverCommited_ShouldOptimizeImages()
        {
            var repository = Substitute.For<IRepository>();

            var commits = new SimpleCommitLog(new[]
            {
                OneRandoCommit(new DateTime(2017, 10, 31)),
                OneRandoCommit(new DateTime(2017, 10, 30)),
                OneRandoCommit(new DateTime(2017, 10, 29)),
            });

            repository.Commits.Returns(commits);

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_dailyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_weeklyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_monthlyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(new RepoConfiguration(), repository));
        }

        [TestMethod]
        public void GivenImgBotCommittedYesterday_ShouldOptimizeImagesForDaily()
        {
            var repository = Substitute.For<IRepository>();

            var commits = new SimpleCommitLog(new[]
            {
                OneRandoCommit(DateTime.Now),
                OneImgbotCommit(DateTime.Now - TimeSpan.FromHours(35)),
                OneRandoCommit(new DateTime(2017, 10, 29)),
                OneImgbotCommit(new DateTime(2017, 10, 27)),
                OneImgbotCommit(new DateTime(2017, 10, 25)),
                OneRandoCommit(new DateTime(2017, 10, 20)),
            });

            repository.Commits.Returns(commits);

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_dailyConfiguration, repository));

            Assert.IsFalse(Schedule.ShouldOptimizeImages(_weeklyConfiguration, repository));

            Assert.IsFalse(Schedule.ShouldOptimizeImages(_monthlyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(new RepoConfiguration(), repository));
        }

        [TestMethod]
        public void GivenImgBotCommittedLastWeek_ShouldOptimizeImagesForDailyWeekly()
        {
            var repository = Substitute.For<IRepository>();

            var commits = new SimpleCommitLog(new[]
            {
                OneRandoCommit(DateTime.Now),
                OneImgbotCommit(DateTime.Now - TimeSpan.FromDays(9)),
                OneRandoCommit(new DateTime(2017, 8, 29)),
                OneImgbotCommit(new DateTime(2017, 8, 27)),
                OneImgbotCommit(new DateTime(2017, 8, 25)),
                OneRandoCommit(new DateTime(2017, 8, 20)),
            });

            repository.Commits.Returns(commits);

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_dailyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_weeklyConfiguration, repository));

            Assert.IsFalse(Schedule.ShouldOptimizeImages(_monthlyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(new RepoConfiguration(), repository));
        }

        [TestMethod]
        public void GivenImgBotCommittedLastMonth_ShouldOptimizeImagesForDailyWeeklyMonthly()
        {
            var repository = Substitute.For<IRepository>();

            var commits = new SimpleCommitLog(new[]
            {
                OneRandoCommit(DateTime.Now),
                OneRandoCommit(DateTime.Now - TimeSpan.FromDays(3)),
                OneImgbotCommit(DateTime.Now - TimeSpan.FromDays(40)),
                OneRandoCommit(new DateTime(2017, 8, 29)),
                OneImgbotCommit(new DateTime(2017, 8, 27)),
                OneImgbotCommit(new DateTime(2017, 8, 25)),
                OneRandoCommit(new DateTime(2017, 8, 20)),
            });

            repository.Commits.Returns(commits);

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_dailyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_weeklyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_monthlyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(new RepoConfiguration(), repository));
        }

        [TestMethod]
        public void GivenImgBotCommittedLastYear_ShouldOptimizeImages()
        {
            var repository = Substitute.For<IRepository>();

            var commits = new SimpleCommitLog(new[]
            {
                OneRandoCommit(new DateTime(2017, 10, 31)),
                OneImgbotCommit(new DateTime(2016, 10, 30)),
                OneRandoCommit(new DateTime(2015, 10, 29)),
            });

            repository.Commits.Returns(commits);

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_dailyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_weeklyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(_monthlyConfiguration, repository));

            Assert.IsTrue(Schedule.ShouldOptimizeImages(new RepoConfiguration(), repository));
        }

        private Commit OneImgbotCommit(DateTime when)
        {
            var author = new Signature(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail, when);
            var commit = Substitute.For<Commit>();
            commit.Author.Returns(author);

            return commit;
        }

        private Commit OneRandoCommit(DateTime when)
        {
            var author = new Signature("random", "random@gmail.com", when);
            var commit = Substitute.For<Commit>();
            commit.Author.Returns(author);

            return commit;
        }
    }
}
