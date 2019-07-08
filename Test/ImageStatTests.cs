using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenPrFunction;

namespace Test
{
    [TestClass]
    public class ImageStatTests
    {
        [TestMethod]
        public void ShouldConvertToDouble()
        {
            Assert.AreEqual(1283.3, ImageStat.ToDouble("1,283.30kb"));
            Assert.AreEqual(283.3, ImageStat.ToDouble("283.30kb"));
            Assert.AreEqual(283.0, ImageStat.ToDouble("283kb"));
            Assert.AreEqual(1283.0, ImageStat.ToDouble("1,283kb"));
            Assert.AreEqual(0.0, ImageStat.ToDouble("0kb"));
        }
    }
}
