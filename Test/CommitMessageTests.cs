using System;
using System.IO;
using Common;
using CompressImagesFunction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
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
                    SizeBefore = 1400.3234,
                    SizeAfter = 603.1321987
                },
                new CompressionResult
                {
                    Title = "path/to/image3.png",
                    SizeBefore = 2621440.06,
                    SizeAfter = 2228224.051
                },
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = KnownGitHubs.CommitMessageTitle + Environment.NewLine +
                           Environment.NewLine +
                           "*Total -- 2.50GiB -> 2.13GiB (15.02%)" + Environment.NewLine +
                           Environment.NewLine +
                           "path/to/image2.png -- 1.37MiB -> 603.13KiB (56.93%)" + Environment.NewLine +
                           "path/to/image3.png -- 2.50GiB -> 2.13GiB (15%)" + Environment.NewLine +
                           "path/to/image.png -- 100.37KiB -> 95.79KiB (4.56%)" + Environment.NewLine +
                           Environment.NewLine +
                           "Signed-off-by: ImgBotApp <ImgBotHelp@gmail.com>" + Environment.NewLine;

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

            var expectedMessage = KnownGitHubs.CommitMessageTitle + Environment.NewLine +
                             Environment.NewLine +
                             "path/to/image.png -- 100.30KiB -> 95.70KiB (4.59%)" + Environment.NewLine +
                             Environment.NewLine +
                             "Signed-off-by: ImgBotApp <ImgBotHelp@gmail.com>" + Environment.NewLine;

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

            var expectedMessage = KnownGitHubs.CommitMessageTitle + Environment.NewLine +
                            Environment.NewLine +
                            "*Total -- 501.89KiB -> 455.68KiB (9.21%)" + Environment.NewLine +
                            Environment.NewLine +
                            "path/to/image2.png -- 300.92KiB -> 260.56KiB (13.41%)" + Environment.NewLine +
                            "path/to/image.png -- 200.97KiB -> 195.12KiB (2.91%)" + Environment.NewLine +
                            Environment.NewLine +
                            "Signed-off-by: ImgBotApp <ImgBotHelp@gmail.com>" + Environment.NewLine;

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

            var expectedMessage = KnownGitHubs.CommitMessageTitle + Environment.NewLine +
                        Environment.NewLine +
                        "*Total -- 501.89KiB -> 455.68KiB (9.21%)" + Environment.NewLine
                        + Environment.NewLine +
                        "path/to/image2.png -- 300.92KiB -> 260.56KiB (13.41%)" + Environment.NewLine +
                        "path/to/_image.png -- 200.97KiB -> 195.12KiB (2.91%)" + Environment.NewLine +
                        Environment.NewLine +
                        "Signed-off-by: ImgBotApp <ImgBotHelp@gmail.com>" + Environment.NewLine;

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

        [TestMethod]
        public void GivenTwoCompressionResultArrays_ShouldCreateMergedArray()
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

            var images2 = new[]
            {
                new CompressionResult
                {
                    Title = "path/to/image2.png",
                    SizeBefore = 101.3678,
                    SizeAfter = 96.78743
                },
                new CompressionResult
                {
                    Title = "path/to/image3.png",
                    SizeBefore = 300.3234,
                    SizeAfter = 760.1321987
                },
            };

            var expected = new[]
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
                new CompressionResult
                {
                    Title = "path/to/image3.png",
                    SizeBefore = 300.3234,
                    SizeAfter = 760.1321987
                },
            };

            var mergeResult = CompressionResult.Merge(images, images2);
            Assert.AreEqual(mergeResult.Length, expected.Length);

            for (int i = 0; i < mergeResult.Length; ++i)
            {
                Assert.AreEqual(mergeResult[i].Title, expected[i].Title);
                Assert.AreEqual(mergeResult[i].SizeBefore, expected[i].SizeBefore);
                Assert.AreEqual(mergeResult[i].SizeAfter, expected[i].SizeAfter);
            }
        }

        [TestMethod]
        public void GivenCompressionResultAndFilterArray_ShouldCorrectlyFilter()
        {
            var images = new[]
            {
                new CompressionResult
                {
                    Title = "image.png",
                    SizeBefore = 100.3678,
                    SizeAfter = 95.78743
                },
                new CompressionResult
                {
                    Title = "image2.png",
                    SizeBefore = 500.3234,
                    SizeAfter = 360.1321987
                },
                new CompressionResult
                {
                    Title = "image3.png",
                    SizeBefore = 500.3234,
                    SizeAfter = 360.1321987
                },
            };

            var filter = new[] { Path.Combine("path", "to", "image3.png") };

            var expected = new[]
            {
                new CompressionResult
                {
                    Title = "image.png",
                    SizeBefore = 100.3678,
                    SizeAfter = 95.78743
                },
                new CompressionResult
                {
                    Title = "image2.png",
                    SizeBefore = 500.3234,
                    SizeAfter = 360.1321987
                },
            };

            var filterResult = CompressionResult.Filter(images, filter);
            Assert.AreEqual(filterResult.Length, expected.Length);

            for (int i = 0; i < filterResult.Length; ++i)
            {
                Assert.AreEqual(filterResult[i].Title, expected[i].Title);
                Assert.AreEqual(filterResult[i].SizeBefore, expected[i].SizeBefore);
                Assert.AreEqual(filterResult[i].SizeAfter, expected[i].SizeAfter);
            }
        }

        [TestMethod]
        public void GivenACommitMessage_ShouldCorrectlyParse()
        {
            var commitMessage = KnownGitHubs.CommitMessageTitle + Environment.NewLine +
                        Environment.NewLine +
                        "*Total -- 501.89KiB -> 455.68KiB (9.21%)" + Environment.NewLine
                        + Environment.NewLine +
                        "path/to/image.png -- 200.97KiB -> 195.12KiB (2.91%)" + Environment.NewLine +
                        "path/to/image2.png -- 300.92KiB -> 260.56KiB (13.41%)" + Environment.NewLine +
                        Environment.NewLine +
                        "Signed-off-by: ImgBotApp <ImgBotHelp@gmail.com>" + Environment.NewLine;

            var expected = new[]
            {
                new CompressionResult
                {
                    Title = "path/to/image.png",
                    SizeBefore = 200.97,
                    SizeAfter = 195.12
                },
                new CompressionResult
                {
                    Title = "path/to/image2.png",
                    SizeBefore = 300.92,
                    SizeAfter = 260.56
                },
            };

            var parsed = CommitMessage.Parse(commitMessage);
            Assert.AreEqual(parsed.Length, expected.Length);

            for (int i = 0; i < parsed.Length; ++i)
            {
                Assert.AreEqual(parsed[i].Title, expected[i].Title);
                Assert.AreEqual(parsed[i].SizeBefore, expected[i].SizeBefore);
                Assert.AreEqual(parsed[i].SizeAfter, expected[i].SizeAfter);
            }
        }
    }
}
