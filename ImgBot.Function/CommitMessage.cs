using System.Linq;
using System.Text;
using ImgBot.Common;

namespace ImgBot.Function
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

            return commitMessage.ToString();
        }
    }
}
