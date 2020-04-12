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
            if (repoConfiguration.MinKBReduced == null || repoConfiguration.MinKBReduced <= 0)
            {
                // no threshold specified - let's continue
                return true;
            }

            // determine total KB reduced
            var totalKBReduced = compressionResults.Sum(x => x.SizeBefore - x.SizeAfter);
            return repoConfiguration.MinKBReduced <= totalKBReduced;
        }
    }
}
