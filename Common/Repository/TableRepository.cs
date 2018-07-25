using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.Repository
{
    public class TableRepository : IRepository
    {
        private readonly CloudTableClient _client;

        public TableRepository(CloudTableClient client)
        {
            _client = client;
        }

        public async Task InsertOrMergeAsync<T>(T entity)
            where T : TableEntity, new()
        {
            var table = await GetTableAsync<T>();
            var insertOperation = TableOperation.InsertOrMerge(entity);

            await table.ExecuteAsync(insertOperation);
        }

        public async Task<T> RetrieveAsync<T>(string partitionKey, string rowKey)
            where T : TableEntity, new()
        {
            var table = await GetTableAsync<T>();
            var retrieveOperation = TableOperation.Retrieve<T>(partitionKey, rowKey);

            var result = await table.ExecuteAsync(retrieveOperation);
            return (T)result.Result;
        }

        public async Task DeleteAsync<T>(string partitionKey, string rowKey)
            where T : TableEntity, new()
        {
            var table = await GetTableAsync<T>();
            var entity = await RetrieveAsync<T>(partitionKey, rowKey);
            var operation = TableOperation.Delete(entity);

            await table.ExecuteAsync(operation);
        }

        public async Task<T[]> RetrievePartitionAsync<T>(string partitionKey)
            where T : TableEntity, new()
        {
            var table = await GetTableAsync<T>();

            var result = new List<T>();
            var query = new TableQuery<T>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            query.TakeCount = 100;

            TableContinuationToken tableContinuationToken = null;
            do
            {
                var queryResponse = await table.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
                tableContinuationToken = queryResponse.ContinuationToken;
                result.AddRange(queryResponse.Results);
            }
            while (tableContinuationToken != null);

            return result.ToArray();
        }

        private async Task<CloudTable> GetTableAsync<T>()
        {
            var table = _client.GetTableReference(typeof(T).Name.ToLower());
            await table.CreateIfNotExistsAsync();

            return table;
        }
    }
}
