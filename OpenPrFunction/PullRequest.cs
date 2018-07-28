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
            var branch = await githubClient.Repository.Branch.Get(parameters.RepoOwner, parameters.RepoName, KnownGitHubs.BranchName);
            var commit = await githubClient.Repository.Commit.Get(parameters.RepoOwner, parameters.RepoName, branch.Commit.Sha);

            if (branch != null)
            {
                var pr = new NewPullRequest(KnownGitHubs.CommitMessageTitle, KnownGitHubs.BranchName, repo.DefaultBranch)
                {
                    Body = PullRequestBody.Generate(commit.Commit.Message),
                };

                await githubClient.PullRequest.Create(parameters.RepoOwner, parameters.RepoName, pr);
            }
        }
    }
}
