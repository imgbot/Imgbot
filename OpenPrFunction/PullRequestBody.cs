using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenPrFunction
{
    public static class PullRequestBody
    {
        /*
         * The commit message is the source of truth for the PR we are opening
         * Convert this format
         *
         *      [ImgBot] optimizes images

                *Total -- 854.23kb -> 308.28kb (63.91%)

                /featurecard.png -- 542.34kb -> 86.13kb (84.12%)
                /graph.png -- 148.78kb -> 88.71kb (40.38%)
                /featured-marketplace.png -- 163.11kb -> 133.44kb (18.19%)
         *
         *
         */
        public static string Generate(string commitBody)
        {
            try
            {
                var imageStats = new List<ImageStat>();

                var commitLines = commitBody.Split(new[] { '\r', '\n' });
                for (var i = 0; i < commitLines.Length; i++)
                {
                    if (i == 0 || string.IsNullOrWhiteSpace(commitLines[i]))
                    {
                        // skip the first line and blank lines
                        continue;
                    }

                    var pattern = @"(.*) -- (.*) -> (.*) \((.*)\)";
                    var capture = Regex.Matches(commitLines[i], pattern)[0];

                    imageStats.Add(new ImageStat
                    {
                        Name = capture.Groups[1].Value,
                        Before = capture.Groups[2].Value,
                        After = capture.Groups[3].Value,
                        Percent = capture.Groups[4].Value,
                    });
                }

                var sb = new StringBuilder();

                sb.AppendLine("Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information.");
                sb.AppendLine();

                if (imageStats.Count > 0)
                {
                    sb.AppendLine("<details>");

                    sb.AppendLine("<summary>");
                    sb.AppendLine("Compression result");
                    sb.AppendLine("</summary>");
                    sb.AppendLine();

                    sb.AppendLine("| | Before | After | Percent reduction |");
                    sb.AppendLine("|:--|:--|:--|:--|");

                    for (var i = 0; i < imageStats.Count; i++)
                    {
                        sb.AppendLine($"| {imageStats[i].Name} | {imageStats[i].Before} | {imageStats[i].After} | {imageStats[i].Percent} |");

                        if (imageStats.Count > 1 && i == 0)
                        {
                            // print separator line between summary and individual stats
                            // there is only a summary if there is more than one image
                            sb.AppendLine("| | | | |");
                        }
                    }

                    sb.AppendLine("</details>");
                }

                return sb.ToString();
            }
            catch
            {
                return "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information."
                    + Environment.NewLine + Environment.NewLine;
            }
        }

        private class ImageStat
        {
            public string Name { get; set; }

            public string Before { get; set; }

            public string After { get; set; }

            public string Percent { get; set; }
        }
    }
}
