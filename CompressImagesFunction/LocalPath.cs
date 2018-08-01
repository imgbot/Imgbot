using System;
using System.IO;

namespace CompressImagesFunction
{
    public static class LocalPath
    {
        public static string CloneDir(string functionDirectory, string repoName)
        {
            return Path.Combine(functionDirectory, repoName + new Random().Next(100, 99999).ToString());
        }
    }
}
