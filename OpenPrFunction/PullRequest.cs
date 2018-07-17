using System.Threading.Tasks;
using Common;
using Octokit;
using Octokit.Internal;

namespace OpenPrFunction
{
    public static class PullRequest
    {
        public static async Task OpenAsync(PullRequestParameters parameters)
        {
            var inMemoryCredentialStore = new InMemoryCredentialStore(new Credentials(KnownGitHubs.Username, parameters.Password));

            var githubClient = new GitHubClient(new ProductHeaderValue("ImgBot"), inMemoryCredentialStore);

            var repo = await githubClient.Repository.Get(parameters.RepoOwner, parameters.RepoName);

            var pr = new NewPullRequest(KnownGitHubs.CommitMessageTitle, KnownGitHubs.BranchName, repo.DefaultBranch)
            {
                Body = "Beep boop. Optimizing your images is my life. https://imgbot.net/ for more information."
            };

            await githubClient.PullRequest.Create(parameters.RepoOwner, parameters.RepoName, pr);
        }
    }
}
