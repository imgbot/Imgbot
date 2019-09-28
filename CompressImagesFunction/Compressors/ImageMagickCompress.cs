using ImageMagick;

namespace CompressImagesFunction.Compressors
{
    public class ImageMagickCompress : ICompress
    {
        private ImageOptimizer _imageOptimizer;

        public ImageMagickCompress()
        {
            _imageOptimizer = new ImageOptimizer
            {
                OptimalCompression = true,
                IgnoreUnsupportedFormats = true,
            };
        }

        public string[] SupportedExtensions => new[] { ".png", ".jpg", ".jpeg", ".gif" };

        public void LosslessCompress(string path)
        {
            _imageOptimizer.LosslessCompress(path);
        }

        public void LossyCompress(string path)
        {
            _imageOptimizer.Compress(path);
        }
    }
}
