using System;
using System.Collections.Generic;
using System.Text;
using ImgBot.Common;

namespace ImgBot.Function
{
    public static class CommitMessage
    {
        public static string Create(Dictionary<string, Tuple<double, double>> optimizedImages)
        {
            if(optimizedImages == null)
            {
                return string.Empty;
            }

            var commitMessage = new StringBuilder();
            var totalOrigKb = 0.0;
            var totalOptKb = 0.0;
            commitMessage.AppendLine(KnownGitHubs.CommitMessageTitle);

            var imageLog = new StringBuilder();
            foreach (var optimizedImage in optimizedImages.Keys)
            {
                var percent = (1 - (optimizedImages[optimizedImage].Item2 / optimizedImages[optimizedImage].Item1)) * 100;
                imageLog.AppendFormat("{0} -- {1}kb -> {2}kb ({3:0.##}%)", optimizedImage, optimizedImages[optimizedImage].Item1, optimizedImages[optimizedImage].Item2, percent);
                imageLog.AppendLine();
                totalOrigKb += optimizedImages[optimizedImage].Item1;
                totalOptKb += optimizedImages[optimizedImage].Item2;
            }

            if (optimizedImages.Keys.Count > 1)
            {
                var totalPercent = (1 - (totalOptKb / totalOrigKb)) * 100;
                commitMessage.AppendFormat("*Total: {0}kb -> {1}kb ({2:0.##}%)", totalOrigKb, totalOptKb, totalPercent);
                commitMessage.AppendLine();
                commitMessage.AppendLine();
            }

            commitMessage.Append(imageLog);

            return commitMessage.ToString();
        }
    }
}
