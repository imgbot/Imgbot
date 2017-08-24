using System;
using System.IO;
using System.Linq;
using ImageMagick;
using LibGit2Sharp;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ImgBot.Function
{
    public static class Function
    {
        [FunctionName("QueueTrigger")]        
        public static void Run([QueueTrigger("analyze", Connection = "")]AnalyzeMessage analyzeMessage, TraceWriter log, ExecutionContext context)
        {
            log.Info($"C# Queue trigger function processed: {context.FunctionDirectory}");

            var localPath = Path.Combine(context.FunctionDirectory, analyzeMessage.RepoName +  new Random().Next(100,99999).ToString());


            // clone
            Repository.Clone(analyzeMessage.CloneUrl, localPath);


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
                    new UsernamePasswordCredentials { Username = "dabutvin", Password = "XXXXXXXXXXXX" }
            };

            repo.Network.Push(remote, @"refs/heads/imgbot", options);
        }
    }

    public class AnalyzeMessage
    {
        public string CloneUrl { get; set; }
        public string RepoName { get; set; }
    }
}
