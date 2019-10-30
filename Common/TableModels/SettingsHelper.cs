using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace Common.TableModels
{
    public static class SettingsHelper
    {
        public static async Task<Settings> GetSettings(CloudTable table, string installationId, string repoName)
        {
            var query = TableOperation.Retrieve<Settings>(installationId, repoName.ToLower());
            var reply = await table.ExecuteAsync(query);
            return reply?.Result as Settings;
        }

        public static Task<Settings> GetSettings(CloudTable table, int installationId, string repoName)
        {
            return GetSettings(table, installationId.ToString(), repoName);
        }
    }
}
