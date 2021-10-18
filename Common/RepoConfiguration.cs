namespace Common
{
    public class RepoConfiguration
    {
        public string Schedule { get; set; }

        public string[] IgnoredFiles { get; set; }

        public bool AggressiveCompression { get; set; }

        public bool CompressWiki { get; set; }

        public int? MinKBReduced { get; set; } = 10;

        public string PrTitle { get; set; }

        public string PrBody { get; set; }

        // public string Labels { get; set; } TODO: add when having the labels feature
    }
}
