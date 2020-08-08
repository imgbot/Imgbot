using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Common;

namespace CompressImagesFunction
{
    public static class ImageQuery
    {
        public static string[] FindImages(string localPath, RepoConfiguration repoConfiguration)
        {
            var images = Directory.EnumerateFiles(localPath, "*.*", SearchOption.AllDirectories)
                .Where(x => KnownImgPatterns.ImgExtensions.Contains(Path.GetExtension(x).ToLower()))
                .Select(x => x.Replace("\\", "/"));

            return FilterOutIgnoredFiles(images, repoConfiguration);
        }

        public static string[] FilterOutIgnoredFiles(IEnumerable<string> imagePaths, RepoConfiguration repoConfiguration)
        {
            if (repoConfiguration.IgnoredFiles != null)
            {
                foreach (var ignorePattern in repoConfiguration.IgnoredFiles)
                {
                    var pattern = new Regex(NormalizePattern(ignorePattern), RegexOptions.IgnoreCase);
                    imagePaths = imagePaths.Where(x => !pattern.IsMatch(x));
                }
            }

            return imagePaths.ToArray();
        }

        // this is to provide backwards compatibility with the previous globbing
        // that was using only the Directory.EnumerateFiles searchPattern
        private static string NormalizePattern(string ignorePattern)
        {
            ignorePattern = ignorePattern.Replace("\\", "/");
            ignorePattern = ignorePattern.Replace("**", ".*");
            if (ignorePattern.StartsWith("*"))
                return "." + ignorePattern;
            return ignorePattern;
        }
  }
}
