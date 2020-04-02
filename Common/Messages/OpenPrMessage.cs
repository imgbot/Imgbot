namespace Common.Messages
{
    public class OpenPrMessage
    {
        public int InstallationId { get; set; }

        public string RepoName { get; set; }

        public string CloneUrl { get; set; }

        public bool Update { get; set; }
    }
}
