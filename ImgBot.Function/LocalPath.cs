using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImgBot.Function
{
    public static class LocalPath
    {
        public static string CloneDir(string functionDirectory, string repoName)
        {
            return Path.Combine(functionDirectory, repoName + new Random().Next(100, 99999).ToString());
        }
    }
}
