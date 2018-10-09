using System.Threading.Tasks;

namespace OpenPrFunction
{
    public interface IPullRequest
    {
        Task<long> OpenAsync(PullRequestParameters parameters);
    }
}
