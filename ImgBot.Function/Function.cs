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


            // optimize images
            ImageOptimizer imageOptimizer = new ImageOptimizer();
            foreach (var inputPath in images)
            {
                try { imageOptimizer.LosslessCompress(inputPath); } catch { }
            }
        }
    }

    public class AnalyzeMessage
    {
        public string CloneUrl { get; set; }
        public string RepoName { get; set; }
    }
}
