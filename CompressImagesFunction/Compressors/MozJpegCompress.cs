using System.Diagnostics;
using System.IO;

namespace CompressImagesFunction.Compressors
{
    public class MozJpegCompress : ICompress
    {
        private static readonly string LosslessPlugin = "mozjpegtran";
        private static readonly string LossyPlugin = "mozcjpeg";

        public string[] SupportedExtensions =>
            new[] { ".jpg", ".jpeg" };

        public void LosslessCompress(string path)
        {
            var arguments = $"-outfile {path}";
            Compress(LosslessPlugin, arguments);
        }

        public void LossyCompress(string path)
        {
            var tempPath = path + ".tmp";
            var arguments = $"-quality 80 -outfile {tempPath} {path}";

            Compress(LossyPlugin, arguments);

            File.Delete(path);
            File.Move(tempPath, path);
        }

        private void Compress(string compressionType, string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = compressionType,
                Arguments = arguments,
            };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit(10000);
            }
        }
    }
}
