using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Common.Mediation
{
    public class QueueMediator : IMediator
    {
        private readonly CloudQueueClient _queueClient;

        public QueueMediator(CloudQueueClient queueClient)
        {
            _queueClient = queueClient;
        }

        public async Task SendAsync<T>(T message)
        {
            var queue = _queueClient.GetQueueReference(typeof(T).Name.ToLowerInvariant());
            await queue.CreateIfNotExistsAsync();

            await queue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(message)));
        }
    }
}
