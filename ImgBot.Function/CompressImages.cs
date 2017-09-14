using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageMagick;
using ImgBot.Common;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Octokit;
using Octokit.Internal;

namespace ImgBot.Function
{
    public static class CompressImages
    {
        private const string BranchName = "imgbot";
        private const string Username = "x-access-token";

        public static async Task RunAsync(CompressimagesParameters parameters)
        {
            CredentialsHandler credentialsProvider =
                (_url, _user, _cred) =>
                new UsernamePasswordCredentials { Username = Username, Password = parameters.Password };

            InMemoryCredentialStore inMemoryCredentialStore = new InMemoryCredentialStore(new Octokit.Credentials(Username, parameters.Password));

            // clone
            var cloneOptions = 
            LibGit2Sharp.Repository.Clone(parameters.CloneUrl, parameters.LocalPath, new CloneOptions
            {
                CredentialsProvider = credentialsProvider,
            });
            var repo = new LibGit2Sharp.Repository(parameters.LocalPath);
            var remote = repo.Network.Remotes["origin"];

            // check if we have the branch already
            try
            {
                if (repo.Network.ListReferences(remote).Any(x => x.CanonicalName == $"refs/heads/{BranchName}"))
                    return;
            }
            catch
            {
                // ignore
            }

            // check out the branch
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
            repo.Network.Push(remote, $"refs/heads/{BranchName}", new PushOptions
            {
                CredentialsProvider = credentialsProvider,
            });

            // open PR
            var githubClient = new GitHubClient(new ProductHeaderValue("ImgBot"), inMemoryCredentialStore);

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
            Parallel.ForEach(images, image =>
            {
                try
                {
                    Console.WriteLine(image);
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
            });

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
