using System.Threading.Tasks;
using Common;

namespace OpenPrFunction
{
    public interface IPullRequest
    {
        Task<long> OpenAsync(GitHubClientParameters parameters);
    }
}
