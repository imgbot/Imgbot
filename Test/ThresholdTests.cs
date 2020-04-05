using Common;
using CompressImagesFunction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class ThresholdTests
    {
        /// We have a default threshold set so it won't meet it by default
        [TestMethod]
        public void GivenDefaultConfiguration_ShouldNotOptimizeImages()
        {
            var compressionResults = new CompressionResult[] { };
            var configuration = new RepoConfiguration();
            var shouldOptimize = Threshold.MeetsThreshold(configuration, compressionResults);
            Assert.IsFalse(shouldOptimize);
        }

        [TestMethod]
        public void GivenDisabledConfiguration_ShouldOptimizeImages()
        {
            var compressionResults = new CompressionResult[] { };
            var configuration = new RepoConfiguration
            {
                MinKBReduced = null
            };
            var shouldOptimize = Threshold.MeetsThreshold(configuration, compressionResults);
            Assert.IsTrue(shouldOptimize);
        }

        [TestMethod]
        public void Given0_ShouldOptimizeImages()
        {
            var compressionResults = new CompressionResult[] { };
            var configuration = new RepoConfiguration
            {
                MinKBReduced = 0
            };
            var shouldOptimize = Threshold.MeetsThreshold(configuration, compressionResults);
            Assert.IsTrue(shouldOptimize);
        }

        [TestMethod]
        public void GivenBelowThreshold_ShouldOptimizeImages()
        {
            var compressionResults = new CompressionResult[]
            {
                new CompressionResult
                {
                    SizeBefore = 5000,
                    SizeAfter = 4000,
                },
                new CompressionResult
                {
                    SizeBefore = 5000,
                    SizeAfter = 4999,
                },
            };
            var configuration = new RepoConfiguration
            {
                MinKBReduced = 500
            };
            var shouldOptimize = Threshold.MeetsThreshold(configuration, compressionResults);
            Assert.IsTrue(shouldOptimize);
        }

        [TestMethod]
        public void GivenAboveThreshold_ShouldNotOptimizeImages()
        {
            var compressionResults = new CompressionResult[]
            {
                new CompressionResult
                {
                    SizeBefore = 5000,
                    SizeAfter = 4900,
                },
                new CompressionResult
                {
                    SizeBefore = 5000,
                    SizeAfter = 4999,
                },
            };
            var configuration = new RepoConfiguration
            {
                MinKBReduced = 500
            };
            var shouldOptimize = Threshold.MeetsThreshold(configuration, compressionResults);
            Assert.IsFalse(shouldOptimize);
        }
    }
}
