namespace CompressImagesFunction.Compress
{
    public class CompressionRunResult
    {
        public bool DidCompress { get; set; }

        public bool RunNextPage { get; set; }

        public static CompressionRunResult Exit()
        {
            return new CompressionRunResult { DidCompress = false, RunNextPage = false };
        }

        public static CompressionRunResult Success()
        {
            return new CompressionRunResult { DidCompress = true, RunNextPage = false };
        }
    }
}
