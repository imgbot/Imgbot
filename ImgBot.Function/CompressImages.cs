using System;
using System.IO;
using System.Linq;
using ImageMagick;
using LibGit2Sharp;

namespace ImgBot.Function
{
    public static class CompressImages
    {
        private const string BranchName = "imgbot";

        public static void Run(CompressimagesParameters parameters)
        {
            // clone
            Repository.Clone(parameters.CloneUrl, parameters.LocalPath);

            // extract images
            var imgPatterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", };
            var images = imgPatterns.AsParallel().SelectMany(pattern => Directory.EnumerateFiles(parameters.LocalPath, pattern, SearchOption.AllDirectories)).ToArray();

            // check out a branch
            var repo = new Repository(parameters.LocalPath);
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
            var signature = new Signature("imgbot", "imgbot@gmail.com", DateTimeOffset.Now);
            repo.Commit("[ImgBot] optimizes images", signature, signature);

            // push to GitHub
            var remote = repo.Network.Remotes["origin"];

            var options = new PushOptions
            {
                CredentialsProvider = (_url, _user, _cred) =>
                    new UsernamePasswordCredentials { Username = parameters.Username, Password = parameters.Password }
            };

            repo.Network.Push(remote, $"refs/heads/{BranchName}", options);
        }
    }

    public class CompressimagesParameters
    {
        public string LocalPath { get; set; }
        public string CloneUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
