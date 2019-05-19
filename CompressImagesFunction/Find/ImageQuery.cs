using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Common;

namespace CompressImagesFunction.Find
{
    public static class ImageQuery
    {
        public static ImageQueryResult FindImages(string localPath, RepoConfiguration repoConfiguration, string[] updatedImages = null, int page = 1, int pageSize = 1000)
        {
            var images = Directory.EnumerateFiles(localPath, "*.*", SearchOption.AllDirectories)
                .Where(x => KnownImgPatterns.ImgExtensions.Contains(Path.GetExtension(x).ToLower()))
                .Select(x => x.Replace("\\", "/"));

            if (repoConfiguration.IgnoredFiles != null)
            {
                foreach (var ignorePattern in repoConfiguration.IgnoredFiles)
                {
                    var pattern = new Regex(NormalizePattern(ignorePattern), RegexOptions.IgnoreCase);
                    images = images.Where(x => !pattern.IsMatch(x));
                }
            }

            var imagePaths = images
                  .OrderBy(x => x)
                  .Skip((page - 1) * pageSize)
                  .Take(pageSize)
                  .ToArray();

            var hasMoreImages = images.Count() > pageSize + ((page - 1) * pageSize);

            if (hasMoreImages && updatedImages?.Length > 0)
            {
                // if there is more than 1 page and there are updatedImages we want to group them onto this page
                imagePaths = imagePaths.Concat(updatedImages
                    .Select(updated => images.FirstOrDefault(image => image.EndsWith(updated))))
                    .Where(x => x != null)
                    .ToArray();
            }

            return new ImageQueryResult
            {
                ImagePaths = imagePaths,
                HasMoreImages = hasMoreImages,
            };
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
