using ImgBot.Common;
using ImgBot.Function;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImgBot.Test
{
    [TestClass]
    public class CommitMessageTests
    {
        [TestMethod]
        public void GivenImages_ShouldReportEach()
        {
            var images = new[]
            {
                new CompressionResult
                {
                    Title = "path/to/image.png",
                    SizeBefore = 100.3678,
                    SizeAfter = 95.78743
                },
                new CompressionResult
                {
                    Title = "path/to/image2.png",
                    SizeBefore = 500.3234,
                    SizeAfter = 360.1321987
                },
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}

*Total -- 600.69kb -> 455.92kb (24.1%)

path/to/image2.png -- 500.32kb -> 360.13kb (28.02%)
path/to/image.png -- 100.37kb -> 95.79kb (4.56%)
";

            Assert.AreEqual(expectedMessage, message);
        }

        [TestMethod]
        public void GivenOneImage_ShouldReportSingleImageNoTotal()
        {
            var images = new[]
            {
                new CompressionResult
                {
                    Title = "path/to/image.png",
                    SizeBefore = 100.3,
                    SizeAfter = 95.7
                },
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}

path/to/image.png -- 100.30kb -> 95.70kb (4.59%)
";

            Assert.AreEqual(expectedMessage, message);
        }

        [TestMethod]
        public void GivenBackslashes_ShouldRenderForwardSlashes()
        {
            var images = new[]
            {
                new CompressionResult
                {
                    Title = "path\\to\\image.png",
                    SizeBefore = 200.9678,
                    SizeAfter = 195.12365354
                },
                new CompressionResult
                {
                    Title = "path\\to\\image2.png",
                    SizeBefore = 300.9234,
                    SizeAfter = 260.555
                },
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}

*Total -- 501.89kb -> 455.68kb (9.21%)

path/to/image2.png -- 300.92kb -> 260.56kb (13.41%)
path/to/image.png -- 200.97kb -> 195.12kb (2.91%)
";

            Assert.AreEqual(expectedMessage, message);
        }

        [TestMethod]
        public void GivenUnderscorePrefix_ShouldRenderUnderscore()
        {
            var images = new[]
            {
                new CompressionResult
                {
                    Title = "path\\to\\_image.png",
                    SizeBefore = 200.9678,
                    SizeAfter = 195.12365354
                },
                new CompressionResult
                {
                    Title = "path\\to\\image2.png",
                    SizeBefore = 300.9234,
                    SizeAfter = 260.555
                },
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}

*Total -- 501.89kb -> 455.68kb (9.21%)

path/to/image2.png -- 300.92kb -> 260.56kb (13.41%)
path/to/_image.png -- 200.97kb -> 195.12kb (2.91%)
";

            Assert.AreEqual(expectedMessage, message);
        }

        [TestMethod]
        public void GivenNullOrEmptyCompressionResults_ShouldCreateEmptyString()
        {
            var message = CommitMessage.Create(null);
            Assert.AreEqual(string.Empty, message);

            var message2 = CommitMessage.Create(new CompressionResult[0]);
            Assert.AreEqual(string.Empty, message);
        }
    }
}
