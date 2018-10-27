using System;
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
            var images = Directory.EnumerateFiles(localPath, "*.*", SearchOption.AllDirectories)
                .Where(x => KnownImgPatterns.ImgExtensions.Contains(Path.GetExtension(x).ToLower()));

            if (repoConfiguration.IgnoredFiles != null)
            {
                // find all the ignored files and exclude them from the found images
                var ignoredFiles = repoConfiguration.IgnoredFiles
                    .Select(x => x.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
                    .AsParallel()
                    .SelectMany(pattern =>
                    {
                        try
                        {
                            return Directory.EnumerateFiles(localPath, pattern, SearchOption.AllDirectories);
                        }
                        catch
                        {
                            // ignore issues enumerating files
                            return Enumerable.Empty<string>();
                        }
                    });

                images = images.Except(ignoredFiles);
            }

            return images.ToArray();
        }
    }
}
