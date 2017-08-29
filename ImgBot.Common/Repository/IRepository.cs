using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace ImgBot.Common.Repository
{
    public interface IRepository
    {
        Task InsertOrMergeAsync<T>(T entity) where T : TableEntity, new();
        Task<T> RetrieveAsync<T>(string partitionKey, string rowKey) where T : TableEntity, new();
        Task DeleteAsync<T>(string partitionKey, string rowKey) where T : TableEntity, new();
        Task<T[]> RetrievePartitionAsync<T>(string partitionKey) where T : TableEntity, new();
    }
}