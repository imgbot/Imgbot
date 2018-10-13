using Common;
using System.Threading.Tasks;

namespace OpenPrFunction
{
    public interface IPullRequest
    {
        Task<long> OpenAsync(GitHubClientParameters parameters);
    }
}
