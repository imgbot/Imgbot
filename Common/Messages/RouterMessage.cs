namespace Common.Messages
{
    public class RouterMessage
    {
        public int InstallationId { get; set; }

        public string RepoName { get; set; }

        public string CloneUrl { get; set; }

        public string Owner { get; set; }

        public bool IsRebase { get; set; }
    }
}
