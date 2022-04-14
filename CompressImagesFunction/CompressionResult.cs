namespace CompressImagesFunction
{
    public partial class CompressionResult
    {
        public string Title { get; set; }

        public string OriginalPath { get; set; }

        public double SizeBefore { get; set; }

        public double SizeAfter { get; set; }

        public double PercentSaved => (1 - (SizeAfter / SizeBefore)) * 100d;

        public override string ToString()
        {
            string unitBefore = "KiB", unitAfter = "KiB";
            double convSizeBefore = SizeBefore, convSizeAfter = SizeAfter;
            if (SizeBefore / 1048576d >= 1d)
            {
                unitBefore = "GiB";
                convSizeBefore = SizeBefore / 1048576d;
            }
            else if (SizeBefore / 1024d >= 1d)
            {
                unitBefore = "MiB";
                convSizeBefore = SizeBefore / 1024d;
            }

            if (SizeAfter / 1048576d >= 1d)
            {
                unitAfter = "GiB";
                convSizeAfter = SizeAfter / 1048576d;
            }
            else if (SizeAfter / 1024d >= 1d)
            {
                unitAfter = "MiB";
                convSizeAfter = SizeAfter / 1024d;
            }


            return $"{Title.Replace('\\', '/')} -- {convSizeBefore:N2}{unitBefore} -> {convSizeAfter:N2}{unitAfter} ({PercentSaved:0.##}%)";
        }
    }
}
