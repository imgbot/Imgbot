using System.Linq;
using Common;

namespace CompressImagesFunction
{
    public class Threshold
    {
        /// <summary>
        /// Using the compressionResults and the repoConfiguration determine whether
        /// the optimization warrants a PR at this time.
        /// </summary>
        /// <returns>True when the images are compressed enough to warrant a PR.</returns>
        public static bool MeetsThreshold(RepoConfiguration repoConfiguration, CompressionResult[] compressionResults)
        {
            if (repoConfiguration.MinKiBReduced == null || repoConfiguration.MinKiBReduced <= 0)
            {
                // no threshold specified - let's continue
                return true;
            }

            // determine total KiB reduced
            var totalKiBReduced = compressionResults.Sum(x => x.SizeBefore - x.SizeAfter);
            return repoConfiguration.MinKiBReduced <= totalKiBReduced;
        }
    }
}
