namespace Common
{
    public class RepoConfiguration
    {
        public string Schedule { get; set; }

        public string[] IgnoredFiles { get; set; }

        public bool AggressiveCompression { get; set; }

        public bool CompressWiki { get; set; }

        public int? MinKBReduced { get; set; } = 10;
    }
}
