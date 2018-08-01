using System;
using System.Linq;
using Common;

namespace CompressImagesFunction
{
    public class Schedule
    {
        /// <summary>
        /// Using the repo and the repoConfiguration determine whether
        /// the schedule permits an optimization at this time.
        /// </summary>
        /// <param name="repoConfiguration">The configuration for the repository.</param>
        /// <param name="repo">The repository.</param>
        /// <returns>True when the images can be optimized.</returns>
        public static bool ShouldOptimizeImages(RepoConfiguration repoConfiguration, LibGit2Sharp.IRepository repo)
        {
            if (string.IsNullOrEmpty(repoConfiguration.Schedule))
            {
                // no schedule specified - let's optimize those images
                return true;
            }

            // determine backofftime from keywords
            TimeSpan backofftime;
            switch (repoConfiguration.Schedule)
            {
                case KnownScheduleSettings.Daily:
                    backofftime = TimeSpan.FromDays(1);
                    break;
                case KnownScheduleSettings.Weekly:
                    backofftime = TimeSpan.FromDays(7);
                    break;
                case KnownScheduleSettings.Monthly:
                    backofftime = TimeSpan.FromDays(30);
                    break;
                default:
                    backofftime = TimeSpan.Zero;
                    break;
            }

            // find the last time imgbot committed here
            var imgbotCommit = repo.Commits.FirstOrDefault(x => x.Author.Email == KnownGitHubs.ImgBotEmail);

            if (imgbotCommit == null)
            {
                // no imgbot commit in history - let's optimize those images
                return true;
            }

            if (DateTimeOffset.Now - imgbotCommit.Author.When > backofftime)
            {
                // Now minus the last imgbot commit is greater than the backoff time - let's optimize those images
                return true;
            }

            return false;
        }
    }
}
