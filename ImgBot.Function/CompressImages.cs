using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using LibGit2Sharp;
using Octokit;
using Octokit.Internal;

namespace ImgBot.Function
{
    public static class CompressImages
    {
        private const string BranchName = "imgbot";

        public static async Task RunAsync(CompressimagesParameters parameters)
        {
            // clone
            LibGit2Sharp.Repository.Clone(parameters.CloneUrl, parameters.LocalPath);

            // extract images
            var imgPatterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", };
            var images = imgPatterns.AsParallel().SelectMany(pattern => Directory.EnumerateFiles(parameters.LocalPath, pattern, SearchOption.AllDirectories)).ToArray();

            // check out a branch
            var repo = new LibGit2Sharp.Repository(parameters.LocalPath);
            repo.CreateBranch(BranchName);
            var branch = Commands.Checkout(repo, BranchName);

            // optimize images
            ImageOptimizer imageOptimizer = new ImageOptimizer();
            foreach (var image in images)
            {
                try { imageOptimizer.LosslessCompress(image); } catch { }
                Commands.Stage(repo, image);
            }

            // commit
            var signature = new LibGit2Sharp.Signature("imgbot", "imgbot@gmail.com", DateTimeOffset.Now);
            repo.Commit("[ImgBot] optimizes images", signature, signature);

            // push to GitHub
            var remote = repo.Network.Remotes["origin"];

            var options = new PushOptions
            {
                CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = parameters.Username, Password = parameters.Password }
            };

            repo.Network.Push(remote, $"refs/heads/{BranchName}", options);


            // open PR
            var credentials = new InMemoryCredentialStore(new Octokit.Credentials(parameters.Username, parameters.Password));
            var githubClient = new GitHubClient(new ProductHeaderValue("ImgBot"), credentials);

            var pr = new NewPullRequest("[ImgBot] Optimizes Images", BranchName, "master");
            pr.Body = "Beep boop. Optimizing your images is my life";
            await githubClient.PullRequest.Create(parameters.RepoOwner, parameters.RepoName, pr);
        }
    }

    public class CompressimagesParameters
    {
        public string RepoOwner { get; set; }
        public string RepoName { get; set; }
        public string LocalPath { get; set; }
        public string CloneUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
