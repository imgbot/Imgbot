using System.Diagnostics;

namespace CompressImagesFunction.Compressors
{
    /*
    -O[level]
    --optimize[=level]
        Optimize  output GIF animations for space.  Level determines how much optimization is
        done; higher levels take longer, but may have better  results.  There  are  currently
        three levels:

        -O1  Stores only the changed portion of each image. This is the default.
        -O2  Also uses transparency to shrink the file further.
        -O3  Try several optimization methods (usually slower, sometimes better results).

    To modify GIF files in place, you should use the --batch option.  With  --batch,  gifsicle
    will modify the files you specify instead of writing a new file to the standard output.
    */
    public class GifsicleCompress : ICompress
    {
        public string[] SupportedExtensions => new[] { ".gif" };

        public void LosslessCompress(string path)
        {
            var arguments = $"-O3 --batch {path}";
            Compress(arguments);
        }

        public void LossyCompress(string path)
        {
            var arguments = $"-O3 --lossy=80 --batch {path}";
            Compress(arguments);
        }

        private void Compress(string arguments)
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "gifsicle",
                Arguments = arguments,
            };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit(10000);
            }
        }
    }
}
