using System.Threading.Tasks;
using Common;
using Common.TableModels;

namespace OpenPrFunction
{
    public interface IPullRequest
    {
        Task<Pr> OpenAsync(GitHubClientParameters parameters, bool update, Settings settings = null);
    }
}
