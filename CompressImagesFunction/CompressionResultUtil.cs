using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompressImagesFunction
{
    public partial class CompressionResult
    {
        public static CompressionResult[] ParseCommitMessage(string commitBody)
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

        public static CompressionResult[] Merge(CompressionResult[] newOptimizedImages, CompressionResult[] previousCommitResults)
        {
            List<CompressionResult> list = new List<CompressionResult>();
            list.AddRange(newOptimizedImages);
            list.AddRange(previousCommitResults);

            var nonRepeat = list.GroupBy(x => x.Title).Select(y => y.First());

            return nonRepeat.ToArray();
        }

        public static CompressionResult[] Filter(CompressionResult[] optimizedImages, string[] toRemove)
        {
            var relativePaths = toRemove.Select(path => Path.GetFileName(path));
            var filtered = optimizedImages.Where(r => !relativePaths.Contains(r.Title));
            return filtered.ToArray();
        }
    }
}
