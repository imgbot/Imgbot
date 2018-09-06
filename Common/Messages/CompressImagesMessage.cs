namespace Common.Messages
{
    public class CompressImagesMessage
    {
        public string CloneUrl { get; set; }

        public string RepoName { get; set; }

        public string Owner { get; set; }

        public int InstallationId { get; set; }
    }
}
