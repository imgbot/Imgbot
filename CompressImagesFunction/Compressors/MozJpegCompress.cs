using System;
using System.Diagnostics;
using System.Linq;

namespace CompressImagesFunction.Compressors
{
    public class MozJpegCompress : ICompress
    {
        private static string[] losslessPlugins = new[] { "jpegtran", };
        private static string[] lossyPlugins = losslessPlugins.Concat(new[] { "cjpeg", }).ToArray();

        public string[] SupportedExtensions => new[] { ".jpg", ".jpeg" };

        public void LosslessCompress(string path)
        {
            Compress(path, losslessPlugins);
        }

        public void LossyCompress(string path)
        {
            Compress(path, lossyPlugins);
        }

        private void Compress(string path, string[] plugins)
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "mozjpeg",
                Arguments = $"{path} --config=\"{{\"\"full\"\":true}}\" -quality --enable={string.Join(",", plugins)}"
            };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit(10000);
            }
        }
    }
}
