using Microsoft.WindowsAzure.Storage.Table;

namespace Common.TableModels
{
    public class Settings : TableEntity
    {
        public Settings()
        {
        }

        public Settings(string installationId, string repoName)
        {
            PartitionKey = installationId;
            RowKey = repoName.ToLower();
            InstallationId = installationId;
            RepoName = repoName;
        }

        public string InstallationId { get; set; }

        public string RepoName { get; set; }

        public string DefaultBranchOverride { get; set; }

        public string PrBody { get; set; }

        public string PrTitle { get; set; }

        public string Labels { get; set; }
    }
}
