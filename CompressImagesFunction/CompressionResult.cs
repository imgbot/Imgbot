namespace CompressImagesFunction
{
    public partial class CompressionResult
    {
        public string Title { get; set; }

        public string OriginalPath { get; set; }

        public double SizeBefore { get; set; }

        public double SizeAfter { get; set; }

        public double PercentSaved => (1 - (SizeAfter / SizeBefore)) * 100d;

        public override string ToString() =>
            $"{Title.Replace('\\', '/')} -- {SizeBefore:N2}kb -> {SizeAfter:N2}kb ({PercentSaved:0.##}%)";
    }
}
