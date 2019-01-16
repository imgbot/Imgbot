using System.Threading.Tasks;
using Common;
using Octokit;
using Octokit.Internal;

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

            try
            {
                var repo = await githubClient.Repository.Get(
                    parameters.RepoOwner, parameters.RepoName);
                return repo.Archived;
            }
            catch (NotFoundException)
            {
                return true; // not found can be treated the same as archived
            }
        }
    }
}
