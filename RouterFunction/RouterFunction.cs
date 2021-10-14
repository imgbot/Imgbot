using System;
using Common.Messages;
using Common.TableModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace RouterFunction
{
    public static class RouterFunction
    {
        [FunctionName("RouterFunction")]
        public static void Run(
            [QueueTrigger("routermessage")]RouterMessage routerMessage,
            [Table("installation", "{InstallationId}", "{RepoName}")] Installation installation,
            [Table("installation")] ICollector<Installation> installations,
            [Queue("compressimagesmessage")] ICollector<CompressImagesMessage> compressImagesMessages,
            ILogger logger)
        {
            var compress = routerMessage.GetType().GetProperty("Compress") == null || routerMessage.Compress == true;

            if (installation == null)
            {
                installations.Add(new Installation(routerMessage.InstallationId, routerMessage.RepoName)
                {
                    CloneUrl = routerMessage.CloneUrl,
                    Owner = routerMessage.Owner,
                    LastChecked = DateTime.UtcNow,
                    IsOptimized = compress,
                    IsPrivate = routerMessage.IsPrivate,
                });
            }
            else
            {
                installation.LastChecked = DateTime.UtcNow;
            }

            if (routerMessage.GetType().GetProperty("Update") != null && routerMessage.Update == true)
            {
                installation.IsOptimized = compress;
            }

            /*
             *  TODO: add logic for routing
             *        https://github.com/dabutvin/ImgBot/issues/98
             */
            if (compress)
            {
                compressImagesMessages.Add(new CompressImagesMessage
                {
                    CloneUrl = routerMessage.CloneUrl,
                    InstallationId = routerMessage.InstallationId,
                    Owner = routerMessage.Owner,
                    RepoName = routerMessage.RepoName,
                });

                logger.LogInformation("RouterFunction: Added CompressImagesMessage for {Owner}/{RepoName}", routerMessage.Owner, routerMessage.RepoName);
            }
        }
    }
}
