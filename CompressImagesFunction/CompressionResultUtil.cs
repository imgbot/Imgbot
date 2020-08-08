using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompressImagesFunction
{
    public partial class CompressionResult
    {
        // If items with the same "Title" are found, the merge will ignore the entry from array2
        public static CompressionResult[] Merge(CompressionResult[] array1, CompressionResult[] array2)
        {
            List<CompressionResult> list = new List<CompressionResult>();
            list.AddRange(array1);
            list.AddRange(array2);

            var nonRepeat = list.GroupBy(x => x.Title).Select(y => y.First());

            return nonRepeat.ToArray();
        }

        public static CompressionResult[] Filter(CompressionResult[] optimizedImages, string[] toRemove)
        {
            var relativePaths = toRemove.Select(path => Path.GetFileName(path));
            var filtered = optimizedImages.Where(r => !relativePaths.Contains(r.Title));
            return filtered.ToArray();
        }
    }
}
