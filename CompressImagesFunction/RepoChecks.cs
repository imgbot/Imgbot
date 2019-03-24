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

        public async Task<bool> BranchExists(GitHubClientParameters parameters)
        {
            var inMemoryCredentialStore = new InMemoryCredentialStore(
                new Credentials(KnownGitHubs.Username, parameters.Password));

            var githubClient = new GitHubClient(
                new ProductHeaderValue("ImgBot"), inMemoryCredentialStore);
            try
            {
                var branch = await githubClient.Repository.Branch.Get(
                    parameters.RepoOwner, parameters.RepoName, KnownGitHubs.BranchName);
                return branch?.Commit != null;
            }
            catch (NotFoundException)
            {
                return false; // not found can be treated the same as does not exist
            }
        }
    }
}
