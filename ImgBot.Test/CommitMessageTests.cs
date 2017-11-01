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
                ["path/to/image.png"] = Tuple.Create(100.3678, 95.78743),
                ["path/to/image2.png"] = Tuple.Create(500.3234, 360.1321987)
            };

            var message = CommitMessage.Create(images);

            var expectedMessage = $@"{KnownGitHubs.CommitMessageTitle}

*Total: 600.69kb -> 455.92kb (24.1%)

path/to/image.png -- 100.37kb -> 95.79kb (4.56%)
path/to/image2.png -- 500.32kb -> 360.13kb (28.02%)
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

path/to/image.png -- 100.30kb -> 95.70kb (4.59%)
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
