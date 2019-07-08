using System;

namespace OpenPrFunction
{
    public class ImageStat
    {
        public string Name { get; set; }

        public string Before { get; set; }

        public string After { get; set; }

        public double Percent { get; set; }

        // before/after are in the form 1,283.30kb (string)
        // this function will convert to 1283.30 (double)
        public static double ToDouble(string value)
        {
            return Convert.ToDouble(value.Split(new[] { "kb" }, StringSplitOptions.None)[0]);
        }
    }
}
