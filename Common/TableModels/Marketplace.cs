using Microsoft.WindowsAzure.Storage.Table;

namespace Common.TableModels
{
    public class Marketplace : TableEntity
    {
        public Marketplace()
        {
        }

        public Marketplace(int accountId, string accountLogin)
        {
            PartitionKey = accountId.ToString();
            RowKey = accountLogin;

            AccountId = accountId;
            AccountLogin = accountLogin;
        }

        public int AccountId { get; set; }

        public string AccountLogin { get; set; }

        public string AccountType { get; set; }

        public string SenderEmail { get; set; }

        public string OrganizationBillingEmail { get; set; }

        public int? PlanId { get; set; }

        public int? SenderId { get; set; }

        public string SenderLogin { get; set; }

        public bool? Student { get; set; }

        public int? AllowedPrivate { get; set; }

        public int? UsedPrivate { get; set; }

        public bool? FreeTrial { get; set; }
    }
}
