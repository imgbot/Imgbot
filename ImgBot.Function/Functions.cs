using System;
using System.IO;
using ImgBot.Common.Messages;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace ImgBot.Function
{
    public static class Functions
    {
        [FunctionName("QueueTrigger")]
        public static void Run([QueueTrigger("installationmessage", Connection = "")]InstallationMessage analyzeMessage, TraceWriter log, ExecutionContext context)
        {
            var localPath = Path.Combine(context.FunctionDirectory, "portfolio" + new Random().Next(100, 99999).ToString());

            Work.CompressImages("https://github.com/dabutvin/portfolio", localPath);
        }
    }
}
