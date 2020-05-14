using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace CompressImagesFunction
{
    public static class CommitMessage
    {
        public static string Create(CompressionResult[] optimizedImages)
        {
            if (optimizedImages?.Any() != true)
            {
                return string.Empty;
            }

            var commitMessage = new StringBuilder();

            var totalOrigKb = 0.0;
            var totalOptKb = 0.0;
            commitMessage.AppendLine(KnownGitHubs.CommitMessageTitle);
            commitMessage.AppendLine();

            var imageLog = new StringBuilder();
            foreach (var optimizedImage in optimizedImages.OrderByDescending(x => x.PercentSaved))
            {
                imageLog.Append(optimizedImage);
                imageLog.AppendLine();

                totalOrigKb += optimizedImage.SizeBefore;
                totalOptKb += optimizedImage.SizeAfter;
            }

            if (optimizedImages.Length > 1)
            {
                var totalCompression = new CompressionResult
                {
                    Title = "*Total",
                    SizeBefore = totalOrigKb,
                    SizeAfter = totalOptKb,
                };

                commitMessage.Append(totalCompression);
                commitMessage.AppendLine();
                commitMessage.AppendLine();
            }

            commitMessage.Append(imageLog);

            commitMessage.AppendLine();
            commitMessage.AppendLine($"Signed-off-by: {KnownGitHubs.ImgBotLogin} <{KnownGitHubs.ImgBotEmail}>");

            return commitMessage.ToString();
        }

        public static CompressionResult[] Parse(string commitBody)
        {
            try
            {
                var compressionResults = new List<CompressionResult>();

                var commitLines = commitBody.Split(new[] { '\r', '\n' });
                for (var i = 0; i < commitLines.Length; i++)
                {
                    if (i == 0 || string.IsNullOrWhiteSpace(commitLines[i]))
                    {
                        // skip the first line and blank lines
                        continue;
                    }

                    if (commitLines[i].StartsWith("Signed-off-by:") || commitLines[i].StartsWith("*Total --"))
                    {
                        // skip the DCO line
                        continue;
                    }

                    var pattern = @"\*?(.*) -- (.*)kb -> (.*)kb \((.*)%\)";
                    var capture = Regex.Matches(commitLines[i], pattern)[0];

                    compressionResults.Add(new CompressionResult
                    {
                        Title = capture.Groups[1].Value,
                        SizeBefore = Convert.ToDouble(capture.Groups[2].Value),
                        SizeAfter = Convert.ToDouble(capture.Groups[3].Value),
                    });
                }

                return compressionResults.ToArray();
            }
            catch
            {
                // commit messages can be out of our control
                return null;
            }
        }

        public static int ToSecondsSinceEpoch(this DateTimeOffset date)
        {
            var utcDate = date.ToUniversalTime();
            return (int)utcDate.Subtract(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
        }
    }
}
