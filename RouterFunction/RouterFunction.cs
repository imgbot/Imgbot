using System;
using Common.Messages;
using Common.TableModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

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
            TraceWriter log)
        {
            if (installation == null)
            {
                installations.Add(new Installation(routerMessage.InstallationId, routerMessage.RepoName)
                {
                    CloneUrl = routerMessage.CloneUrl,
                    Owner = routerMessage.Owner,
                });
            }

            /*
             *  TODO: add logic for routing
             *        https://github.com/dabutvin/ImgBot/issues/98
             */

            log.Info($"Routing {routerMessage.CloneUrl} to {nameof(compressImagesMessages)}.");

            compressImagesMessages.Add(new CompressImagesMessage
            {
                CloneUrl = routerMessage.CloneUrl,
                InstallationId = routerMessage.InstallationId,
                Owner = routerMessage.Owner,
                RepoName = routerMessage.RepoName,
            });
        }
    }
}
