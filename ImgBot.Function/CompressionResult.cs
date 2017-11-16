namespace ImgBot.Function
{
    public class CompressionResult
    {
        public string FileName { get; set; }
        public double SizeBefore { get; set; }
        public double SizeAfter { get; set; }
        public double PercentSaved => (1 - (SizeAfter / SizeBefore)) * 100d;
    }
}
