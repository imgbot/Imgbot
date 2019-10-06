using System.IO;
using System.Linq;
using Common;
using CompressImagesFunction;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class MozJpegCompressTests
    {
        [TestMethod]
        public void TestPath()
        {
            var image = ImageQuery.FindImages("data", new RepoConfiguration()).First();

            FileInfo file = new FileInfo(image);
            double before = file.Length / 1024d;

            new CompressImagesFunction.Compressors.MozJpegCompress().LosslessCompress(image);
            FileInfo fileAfter = new FileInfo(image);

            Assert.IsTrue(before > fileAfter.Length/1024d);
        }
    }
}
