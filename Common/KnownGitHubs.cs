using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Common
{
    public static class KnownGitHubs
    {
        public const int AppId = 137050;

        public const string ImgBotLogin = "GrigoreMihai";

        public const string ImgBotEmail = "grigoremihai56@gmail.com";

        public const string CommitMessageTitle = "[ImgBot] Optimize images";

        public const string Username = "x-access-token";

        public const string BranchName = "imgbot";

        // <remarks>
        // {0} = installation_id.
        // </remarks>
        public const string AccessTokensUrlFormat = "https://api.github.com/app/installations/{0}/access_tokens";

        public const int SmallestLimitPaidPlan = 7;

        // -1 for existing plans that include unlimited private
        // -2 for old plans which also have unlimited private, that do not need marketplacesync
        // int values represent the number of private repos
        // the last plan we need to edit after creating it in github market place
        public static readonly ReadOnlyDictionary<int, int> Plans
        = new ReadOnlyDictionary<int, int>(
            new Dictionary<int, int>()
            {
                { 781, -2 },
                { 1749, 0 },
                { 1750, -2 },
                { 2840, -1 },
                { 2841, -1 },
                { 6857, 7 },
            });
    }
}
