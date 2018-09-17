using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.TableModels
{
    public static class Extensions
    {
        public static async Task<bool> DropPartitionAsync(this CloudTable table, string partitionKey)
        {
            var result = new List<DynamicTableEntity>();
            var query = new TableQuery().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));
            query.TakeCount = 100;

            TableContinuationToken tableContinuationToken = null;
            do
            {
                var queryResponse = await table.ExecuteQuerySegmentedAsync(query, tableContinuationToken);
                tableContinuationToken = queryResponse.ContinuationToken;
                result.AddRange(queryResponse.Results);
            }
            while (tableContinuationToken != null);

            foreach (var row in result)
            {
                await table.ExecuteAsync(TableOperation.Delete(row));
            }

            return true;
        }

        public static async Task<bool> DropRow(this CloudTable table, string partitionKey, string rowKey)
        {
            var retrieveOperation = TableOperation.Retrieve<Installation>(partitionKey, rowKey);
            var row = await table.ExecuteAsync(retrieveOperation);

            await table.ExecuteAsync(TableOperation.Delete((ITableEntity)row.Result));

            return true;
        }

        public static Task<bool> DropRow(this CloudTable table, int partitionKey, string rowKey)
        {
            return table.DropRow(partitionKey.ToString(), rowKey);
        }
    }
}
