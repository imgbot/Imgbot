using System.Diagnostics;
using System.Linq;

namespace CompressImagesFunction.Compressors
{
    public class SvgoCompress : ICompress
    {
        private static string[] losslessPlugins = new[]
        {
            "cleanupAttrs",
            "cleanupListOfValues",
            "cleanupNumericValues",
            "convertColors",

            // "convertStyleToAttrs", // see issue https://github.com/dabutvin/Imgbot/issues/603
            "minifyStyles",
            "moveGroupAttrsToElems",
            "removeComments",
            "removeDoctype",
            "removeEditorsNSData",
            "removeEmptyAttrs",
            "removeEmptyContainers",
            "removeEmptyText",
            "removeNonInheritableGroupAttrs",
            "removeXMLProcInst",
            "sortAttrs",
        };

        private static string[] lossyPlugins = losslessPlugins.Concat(new[]
        {
            "cleanupEnableBackground",
            "cleanupIDs",
            "collapseGroups",
            "convertPathData",
            "convertShapeToPath",
            "convertTransform",
            "mergePaths",
            "moveElemsAttrsToGroup",
            "removeAttrs",
            "removeDesc",
            "removeDimensions",
            "removeElementsByAttr",
            "removeHiddenElems",
            "removeMetadata",
            "removeRasterImages",
            "removeStyleElement",
            "removeTitle",
            "removeUnknownsAndDefaults",
            "removeUnusedNS",
            "removeUselessDefs",
            "removeUselessStrokeAndFill",
            "removeViewBox",
            "removeXMLNS",
        }).ToArray();

        public string[] SupportedExtensions => new[] { ".svg" };

        public void LosslessCompress(string path)
        {
            Compress(path, losslessPlugins);
        }

        public void LossyCompress(string path)
        {
            Compress(path, lossyPlugins);
        }

        private void Compress(string path, string[] plugins)
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                FileName = "svgo",
                Arguments = $"{path} --config=\"{{\"\"full\"\":true}}\" --multipass --enable={string.Join(",", plugins)}"
            };
            using (var process = Process.Start(processStartInfo))
            {
                process.WaitForExit(10000);
            }
        }
    }
}
