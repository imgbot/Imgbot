using System.Diagnostics;

namespace CompressImagesFunction.Compressors
{
    public class MozJpegCompress : ICompress
    {
        private static readonly string LosslessPlugin = "mozjpegtran";
        private static readonly string LossyPlugin = "mozcjpeg";

        public string[] SupportedExtensions =>
            new[] { ".jpg", ".jpeg" };

        public void LosslessCompress(string path) =>
            Compress(path, LosslessPlugin);

        public void LossyCompress(string path) =>
            Compress(path, LossyPlugin, "-quality 80 ");

        private void Compress(string path, string compressionType, string switches = "")
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = compressionType,
                Arguments = $"{switches}-outfile {path} {path}"

                // FileName = "mozjpeg",
                // Arguments = $"{compressionType} -quality 80 -outfile {path} {path}"
            };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit(10000);
            }
        }
    }
}
