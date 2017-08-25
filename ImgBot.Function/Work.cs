using System;
using System.IO;
using System.Linq;
using ImageMagick;
using LibGit2Sharp;

namespace ImgBot.Function
{
    public static class Work
    {
        public static void CompressImages(string cloneUrl, string localPath)
        {
            // clone
            Repository.Clone(cloneUrl, localPath);

            // extract images
            var imgPatterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", };
            var images = imgPatterns.AsParallel().SelectMany(pattern => Directory.EnumerateFiles(localPath, pattern, SearchOption.AllDirectories)).ToArray();

            // check out a branch
            var repo = new Repository(localPath);
            repo.CreateBranch("imgbot");
            var branch = Commands.Checkout(repo, "imgbot");

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
                    new UsernamePasswordCredentials { Username = "dabutvin", Password = "3a594c384364a54a7e181adbe4599be6d28ec9b6" }
            };

            repo.Network.Push(remote, @"refs/heads/imgbot", options);
        }
    }
}
