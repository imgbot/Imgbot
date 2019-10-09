using Microsoft.WindowsAzure.Storage.Table;

namespace Common.TableModels
{
    public class Settings : TableEntity
    {
        public Settings()
        {
        }

        public Settings(string installationId, string repositoryId)
        {
            PartitionKey = installationId;
            RowKey = repositoryId;
            InstallationId = installationId;
            RepositoryId = repositoryId;
        }

        public string InstallationId { get; set; }

        public string RepositoryId { get; set; }

        public string DefaultBranchOverride { get; set; }
    }
}
