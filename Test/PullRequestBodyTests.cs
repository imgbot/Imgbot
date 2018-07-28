using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPrFunction;

namespace Test
{
    [TestClass]
    public class PullRequestBodyTests
    {
        [TestMethod]
        public void GivenMultiImageCommitMessage_ShouldFormatMarkdownTable()
        {
            var commitMessage = "[ImgBot] optimizes images" + Environment.NewLine +
                         Environment.NewLine +
                         "*Total -- 854.23kb -> 308.28kb (63.91%)" + Environment.NewLine +
                         Environment.NewLine +
                         "/featurecard.png -- 542.34kb -> 86.13kb (84.12%)" + Environment.NewLine +
                         "/graph.png -- 148.78kb -> 88.71kb (40.38%)" + Environment.NewLine +
                         "/featured-marketplace.png -- 163.11kb -> 133.44kb (18.19%)" + Environment.NewLine;

            var expectedMarkdown = "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information." + Environment.NewLine +
                          Environment.NewLine +
                          "<details>" + Environment.NewLine +
                          "<summary>" + Environment.NewLine +
                          "Compression result" + Environment.NewLine +
                          "</summary>" + Environment.NewLine +
                          Environment.NewLine +
                          "| | Before | After | Percent reduction |" + Environment.NewLine +
                          "|:--|:--|:--|:--|" + Environment.NewLine +
                          "| *Total | 854.23kb | 308.28kb | 63.91% |" + Environment.NewLine +
                          "| | | | |" + Environment.NewLine +
                          "| /featurecard.png | 542.34kb | 86.13kb | 84.12% |" + Environment.NewLine +
                          "| /graph.png | 148.78kb | 88.71kb | 40.38% |" + Environment.NewLine +
                          "| /featured-marketplace.png | 163.11kb | 133.44kb | 18.19% |" + Environment.NewLine +
                          "</details>" + Environment.NewLine;

            var result = PullRequestBody.Generate(commitMessage);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void GivenSingleImageCommitMessage_ShouldFormatMarkdownTable()
        {
            var commitMessage = "[ImgBot] optimizes images" + Environment.NewLine +
                         Environment.NewLine +
                         "/featured-marketplace.png -- 163.11kb -> 133.44kb (18.19%)" + Environment.NewLine;

            var expectedMarkdown = "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information." + Environment.NewLine +
                          Environment.NewLine +
                          "<details>" + Environment.NewLine +
                          "<summary>" + Environment.NewLine +
                          "Compression result" + Environment.NewLine +
                          "</summary>" + Environment.NewLine +
                          Environment.NewLine +
                          "| | Before | After | Percent reduction |" + Environment.NewLine +
                          "|:--|:--|:--|:--|" + Environment.NewLine +
                          "| /featured-marketplace.png | 163.11kb | 133.44kb | 18.19% |" + Environment.NewLine +
                          "</details>" + Environment.NewLine;

            var result = PullRequestBody.Generate(commitMessage);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void GivenMultiLineGarbageCommitMessage_ShouldRenderDefaultPrBody()
        {
            var commitMessage = "GARBAGE. something else" + Environment.NewLine + Environment.NewLine +
                               "Some more garbage !!" + Environment.NewLine +
                               "One more garbage" + Environment.NewLine;

            var expectedMarkdown = "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information."
                + Environment.NewLine + Environment.NewLine;

            var result = PullRequestBody.Generate(commitMessage);

            Assert.AreEqual(expectedMarkdown, result);
        }

        [TestMethod]
        public void GivenSingleLineGarbageCommitMessage_ShouldRenderDefaultPrBody()
        {
            var commitMessage = "GARBAGE. something else";

            var expectedMarkdown = "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information."
                + Environment.NewLine + Environment.NewLine;

            var result = PullRequestBody.Generate(commitMessage);

            Assert.AreEqual(expectedMarkdown, result);
        }
    }
}
