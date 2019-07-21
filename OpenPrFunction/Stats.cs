using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OpenPrFunction
{
    public static partial class Stats
    {
        /*
         * The commit message body is the source of truth for the PR we are opening
         * Convert this format into ImageStat[]
         *
         *      [ImgBot] Optimize images

                *Total -- 854.23kb -> 308.28kb (63.91%)

                /featurecard.png -- 542.34kb -> 86.13kb (84.12%)
                /graph.png -- 148.78kb -> 88.71kb (40.38%)
                /featured-marketplace.png -- 163.11kb -> 133.44kb (18.19%)
         *
         */
        public static ImageStat[] ParseStats(string commitBody)
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

                    if (commitLines[i].StartsWith("Signed-off-by:"))
                    {
                        // skip the DCO line
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

                return imageStats.ToArray();
            }
            catch
            {
                // commit messages can be out of our control
                return null;
            }
        }
    }
}
