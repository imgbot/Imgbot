using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using ImgBot.Common;
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

            // check out a branch
            var repo = new LibGit2Sharp.Repository(parameters.LocalPath);
            repo.CreateBranch(BranchName);
            var branch = Commands.Checkout(repo, BranchName);

            // optimize images
            var optimizedImages = OptimizeImages(repo, parameters.LocalPath);
            if (optimizedImages.Count == 0)
                return;

            var commitMessage = CreateCommitMessage(optimizedImages);

            // commit
            var signature = new LibGit2Sharp.Signature("ImgBotApp", "ImgBotHelp@gmail.com", DateTimeOffset.Now);
            repo.Commit(commitMessage, signature, signature);

            // push to GitHub
            var remote = repo.Network.Remotes["origin"];
            var username = "x-access-token";

            var options = new PushOptions
            {
                CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = username, Password = parameters.Password }
            };

            repo.Network.Push(remote, $"refs/heads/{BranchName}", options);


            // open PR
            var credentials = new InMemoryCredentialStore(new Octokit.Credentials(username, parameters.Password));
            var githubClient = new GitHubClient(new ProductHeaderValue("ImgBot"), credentials);

            var pr = new NewPullRequest("[ImgBot] Optimizes Images", BranchName, "master");
            pr.Body = "Beep boop. Optimizing your images is my life";
            await githubClient.PullRequest.Create(parameters.RepoOwner, parameters.RepoName, pr);
        }

        private static Dictionary<string, Percentage> OptimizeImages(LibGit2Sharp.Repository repo, string localPath)
        {
            // extract images
            var images = KnownImgPatterns.ImgPatterns.AsParallel().SelectMany(pattern => Directory.EnumerateFiles(localPath, pattern, SearchOption.AllDirectories)).ToArray();

            var optimizedImages = new Dictionary<string, Percentage>();

            ImageOptimizer imageOptimizer = new ImageOptimizer();
            foreach (var image in images)
            {
                try
                {
                    FileInfo file = new FileInfo(image);
                    double before = file.Length;
                    if (imageOptimizer.LosslessCompress(file))
                    {
                        string fileName = image.Substring(localPath.Length);
                        optimizedImages[fileName] = new Percentage((1 - (file.Length / before)) * 100);
                        Commands.Stage(repo, image);
                    }
                }
                catch { }
            }

            return optimizedImages;
        }

        private static string CreateCommitMessage(Dictionary<string, Percentage> optimizedImages)
        {
            var commitMessage = new StringBuilder();
            commitMessage.AppendLine("[ImgBot] optimizes images");

            foreach (var optimizedImage in optimizedImages.Keys)
            {
                commitMessage.AppendFormat("{0} ({1}))", optimizedImage, optimizedImages[optimizedImage].ToString());
                commitMessage.AppendLine();
            }

            return commitMessage.ToString();
        }
    }

    public class CompressimagesParameters
    {
        public string RepoOwner { get; set; }
        public string RepoName { get; set; }
        public string LocalPath { get; set; }
        public string CloneUrl { get; set; }
        public string Password { get; set; }
    }
}
