using System.Linq;

namespace CompressImagesFunction
{
    public static class Svgo
    {
        public static string[] LosslessPlugins => new[]
        {
            "cleanupAttrs",
            "cleanupListOfValues",
            "cleanupNumericValues",
            "convertColors",
            "convertStyleToAttrs",
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

        public static string[] LossyPlugins => LosslessPlugins.Concat(new[]
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
    }
}
