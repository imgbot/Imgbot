using System;
using System.Collections.Generic;
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
            var images = new Dictionary<string, Tuple<double, double>>
            {
                ["path/to/image.png"] = Tuple.Create(100.3, 95.7),
                ["path/to/image2.png"] = Tuple.Create(500.3, 360.1)
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}
*Total: 600.6kb -> 455.8kb (24.11%)

path/to/image.png -- 100.3kb -> 95.7kb (4.59%)
path/to/image2.png -- 500.3kb -> 360.1kb (28.02%)
";

            Assert.AreEqual(expectedMessage, message);
        }

        [TestMethod]
        public void GivenOneImage_ShouldReportSingleImageNoTotal()
        {
            var images = new Dictionary<string, Tuple<double, double>>
            {
                ["path/to/image.png"] = Tuple.Create(100.3, 95.7),
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}
path/to/image.png -- 100.3kb -> 95.7kb (4.59%)
";

            Assert.AreEqual(expectedMessage, message);
        }

        [TestMethod]
        public void GivenNullImageDictionary_ShouldCreateEmptyString()
        {
            var message = CommitMessage.Create(null);
            Assert.AreEqual("", message);
        }
    }
}
