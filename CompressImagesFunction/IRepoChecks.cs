using Common;
using System.Threading.Tasks;

namespace CompressImagesFunction
{
    public interface IRepoChecks
    {
        Task<bool> IsArchived(GitHubClientParameters parameters);
    }
}
