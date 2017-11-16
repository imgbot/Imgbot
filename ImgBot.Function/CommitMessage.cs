using System.Linq;
using System.Text;
using ImgBot.Common;

namespace ImgBot.Function
{
    public static class CommitMessage
    {
        public static string Create(CompressionResult[] optimizedImages)
        {
            if(optimizedImages?.Any() != true)
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
                imageLog.AppendFormat(
                    "{0} -- {1:N2}kb -> {2:N2}kb ({3:0.##}%)",
                    optimizedImage.FileName,
                    optimizedImage.SizeBefore,
                    optimizedImage.SizeAfter,
                    optimizedImage.PercentSaved);

                imageLog.AppendLine();

                totalOrigKb += optimizedImage.SizeBefore;
                totalOptKb += optimizedImage.SizeAfter;
            }

            if (optimizedImages.Length > 1)
            {
                var totalPercent = (1 - (totalOptKb / totalOrigKb)) * 100;
                commitMessage.AppendFormat(
                    "*Total: {0:N2}kb -> {1:N2}kb ({2:0.##}%)",
                    totalOrigKb,
                    totalOptKb, 
                    totalPercent);

                commitMessage.AppendLine();
                commitMessage.AppendLine();
            }

            commitMessage.Append(imageLog);

            return commitMessage.ToString();
        }
    }
}
