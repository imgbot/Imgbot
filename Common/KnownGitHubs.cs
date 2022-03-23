using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Common
{
    public static class KnownGitHubs
    {
        public const int AppId = 4706;

        public const string ImgBotLogin = "ImgBotApp";

        public const string ImgBotEmail = "ImgBotHelp@gmail.com";

        public const string CommitMessageTitle = "[ImgBot] Optimize images";

        public const string Username = "x-access-token";

        public const string BranchName = "imgbot";

        // <remarks>
        // {0} = installation_id.
        // </remarks>
        public const string AccessTokensUrlFormat = "https://api.github.com/app/installations/{0}/access_tokens";

        public const int SmallestLimitPaidPlan = 1;

        // -1 for existing plans that include unlimited private
        // -2 for old plans which also have unlimited private, that do not need marketplacesync
        // int values represent the number of private repos
        // the last plan we need to edit after creating it in github market place
        public static readonly ReadOnlyDictionary<int, int> Plans
        = new ReadOnlyDictionary<int, int>(
            new Dictionary<int, int>()
            {
                { 781, 0 },
                { 1749, 0 },
                { 6927, 0 },
                { 1750, -2 },
                { 2840, -2 },
                { 2841, -2 },
                { 6894, 5 },
                { 6919, 10 },
                { 6920, 20 },
                { 6921, 50 },
                { 6922, 100 },
                { 6923, 200 },
                { 7386, 1 },
                { 7387, 3 },
                { 7388, 10 },
                { 7389, 25 },
                { 7390, 100 },
            });
    }
}
