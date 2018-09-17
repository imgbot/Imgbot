namespace Common
{
    public static class KnownGitHubs
    {
        public const int AppId = 4706;

        public const string AppPrivateKey = "imgbot.2017-08-23.private-key.pem";

        public const string PGPPrivateKeyFilename = "pgp_private_key.txt";

        public const string PGPPasswordFilename = "pgp_password.txt";

        public const string ImgBotLogin = "ImgBotApp";

        public const string ImgBotEmail = "ImgBotHelp@gmail.com";

        public const string CommitMessageTitle = "[ImgBot] optimizes images";

        public const string Username = "x-access-token";

        public const string BranchName = "imgbot";

        /// <remarks>
        /// {0} = installation_id
        /// </remarks>
        public const string AccessTokensUrlFormat = "https://api.github.com/app/installations/{0}/access_tokens";
    }
}
