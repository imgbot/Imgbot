namespace ImgBot.Common.Messages
{
    public class InstallationMessage
    {
        public int InstallationId { get; set; }
        public string Owner { get; set; }
        public string AccessTokensUrl { get; set; }
        public string CloneUrl { get; set; }
        public string RepoName { get; set; }
    }
}
