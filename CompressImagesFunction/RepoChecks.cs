using Common;
using Octokit;
using Octokit.Internal;
using System.Threading.Tasks;

namespace CompressImagesFunction
{
    public class RepoChecks : IRepoChecks
    {
        public async Task<bool> IsArchived(GitHubClientParameters parameters)
        {
            var inMemoryCredentialStore = new InMemoryCredentialStore(
                new Credentials(KnownGitHubs.Username, parameters.Password));

            var githubClient = new GitHubClient(
                new ProductHeaderValue("ImgBot"), inMemoryCredentialStore);

            var repo = await githubClient.Repository.Get(
                parameters.RepoOwner, parameters.RepoName);

            return repo.Archived;
        }
    }
}
