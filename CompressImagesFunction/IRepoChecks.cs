using System.Threading.Tasks;
using Common;

namespace CompressImagesFunction
{
    public interface IRepoChecks
    {
        Task<bool> IsArchived(GitHubClientParameters parameters);

        Task<bool> BranchExists(GitHubClientParameters parameters);
    }
}
