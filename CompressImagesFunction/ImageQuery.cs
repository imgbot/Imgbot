using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace CompressImagesFunction
{
    public static class ImageQuery
    {
        public static string[] FindImages(string localPath, RepoConfiguration repoConfiguration)
        {
            var images = KnownImgPatterns.ImgPatterns
                .AsParallel()
                .SelectMany(pattern => Directory.EnumerateFiles(localPath, pattern, SearchOption.AllDirectories));

            if (repoConfiguration.IgnoredFiles != null)
            {
                // find all the ignored files and exclude them from the found images
                var ignoredFiles = repoConfiguration.IgnoredFiles
                    .Select(x => x.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
                    .AsParallel()
                    .SelectMany(pattern => Directory.EnumerateFiles(localPath, pattern, SearchOption.AllDirectories));

                images = images.Except(ignoredFiles);
            }

            return images.ToArray();
        }
    }
}
