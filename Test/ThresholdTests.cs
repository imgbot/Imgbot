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
    }
}
