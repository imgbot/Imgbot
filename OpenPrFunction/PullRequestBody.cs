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

                    var pattern = @"\*?(.*) -- (.*) -> (.*) \((.*)%\)";
                    var capture = Regex.Matches(commitLines[i], pattern)[0];

                    imageStats.Add(new ImageStat
                    {
                        Name = capture.Groups[1].Value,
                        Before = capture.Groups[2].Value,
                        After = capture.Groups[3].Value,
                        Percent = Convert.ToDouble(capture.Groups[4].Value),
                    });
                }

                if (imageStats.Count == 0)
                {
                    throw new Exception("No images found in commit message");
                }

                var sb = new StringBuilder();
                sb.AppendLine("## Beep boop. Your images are optimized!");
                sb.AppendLine();

                if (imageStats[0].Percent == 0)
                {
                    sb.AppendLine("Your image file size has been reduced!");
                }
                else
                {
                    sb.AppendLine($"Your image file size has been reduced by **{imageStats[0].Percent:N0}%** 🎉");
                }

                sb.AppendLine();
                sb.AppendLine("<details>");

                sb.AppendLine("<summary>");
                sb.AppendLine("Details");
                sb.AppendLine("</summary>");
                sb.AppendLine();

                sb.AppendLine("| File | Before | After | Percent reduction |");
                sb.AppendLine("|:--|:--|:--|:--|");

                if (imageStats.Count == 1)
                {
                    sb.AppendLine($"| {imageStats[0].Name} | {imageStats[0].Before} | {imageStats[0].After} | {imageStats[0].Percent:N2}% |");
                }
                else
                {
                    // the zeroth item is the total; we print it at the bottom of the table
                    for (var i = 1; i < imageStats.Count; i++)
                    {
                        sb.AppendLine($"| {imageStats[i].Name} | {imageStats[i].Before} | {imageStats[i].After} | {imageStats[i].Percent:N2}% |");
                    }

                    sb.AppendLine("| | | | |");
                    sb.AppendLine($"| **Total :** | **{imageStats[0].Before}** | **{imageStats[0].After}** | **{imageStats[0].Percent:N2}%** |");
                }

                sb.AppendLine("</details>");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();

                sb.Append("[📝docs](https://imgbot.net/docs) | ");
                sb.Append("[:octocat: repo](https://github.com/dabutvin/ImgBot) | ");
                sb.Append("[🙋issues](https://github.com/dabutvin/ImgBot/issues) | ");
                sb.Append("[🏅swag](https://goo.gl/forms/1GX7wlhGEX8nkhGO2) | ");
                sb.Append("[🏪marketplace](https://github.com/marketplace/imgbot)");

                sb.AppendLine();

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

            public double Percent { get; set; }
        }
    }
}
