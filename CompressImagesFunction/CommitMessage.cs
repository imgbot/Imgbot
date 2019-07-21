using System;
using System.Linq;
using System.Text;
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

        public static int ToSecondsSinceEpoch(this DateTimeOffset date)
        {
            var utcDate = date.ToUniversalTime();
            return (int)utcDate.Subtract(new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds;
        }
    }
}
