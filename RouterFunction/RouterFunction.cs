using System;
using Common.Messages;
using Common.TableModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

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
            if (installation == null)
            {
                installations.Add(new Installation(routerMessage.InstallationId, routerMessage.RepoName)
                {
                    CloneUrl = routerMessage.CloneUrl,
                    Owner = routerMessage.Owner,
                    LastChecked = DateTime.UtcNow
                });
            }
            else
            {
                installation.LastChecked = DateTime.UtcNow;
            }

            /*
             *  TODO: add logic for routing
             *        https://github.com/dabutvin/ImgBot/issues/98
             */

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
