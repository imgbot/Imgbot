namespace Common.Messages
{
    public class DeleteBranchMessage
    {
        public int InstallationId { get; set; }

        public string RepoName { get; set; }

        public string Owner { get; set; }

        public string CloneUrl { get; set; }
    }
}
