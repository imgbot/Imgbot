namespace CompressImagesFunction.Compressors
{
    public interface ICompress
    {
        string[] SupportedExtensions { get; }

        void LosslessCompress(string path);

        void LossyCompress(string path);
    }
}
