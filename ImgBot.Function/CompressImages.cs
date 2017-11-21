using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageMagick;
using ImgBot.Common;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Newtonsoft.Json;

namespace ImgBot.Function
{
    public static class CompressImages
    {
        public static bool Run(CompressimagesParameters parameters)
        {
            CredentialsHandler credentialsProvider =
                (url, user, cred) =>
                new UsernamePasswordCredentials { Username = KnownGitHubs.Username, Password = parameters.Password };

            // clone
            var cloneOptions = new CloneOptions
            {
                CredentialsProvider = credentialsProvider,
            };

            Repository.Clone(parameters.CloneUrl, parameters.LocalPath, cloneOptions);

            var repo = new Repository(parameters.LocalPath);
            var remote = repo.Network.Remotes["origin"];

            // check if we have the branch already or this is empty repo
            try
            {
                if (repo.Network.ListReferences(remote, credentialsProvider).Any() == false)
                    return false;

                if (repo.Network.ListReferences(remote, credentialsProvider).Any(x => x.CanonicalName == $"refs/heads/{KnownGitHubs.BranchName}"))
                    return false;
            }
            catch
            {
                // ignore
            }

            var repoConfiguration = new RepoConfiguration();

            try
            {
                // see if .imgbotconfig exists in repo root
                var repoConfigJson = File.ReadAllText(parameters.LocalPath + "\\.imgbotconfig");
                if (!string.IsNullOrEmpty(repoConfigJson))
                {
                    repoConfiguration = JsonConvert.DeserializeObject<RepoConfiguration>(repoConfigJson);
                }
            }
            catch
            {
                // ignore
            }

            if (Schedule.ShouldOptimizeImages(repoConfiguration, repo) == false)
                return false;

            // check out the branch
            repo.CreateBranch(KnownGitHubs.BranchName);
            var branch = Commands.Checkout(repo, KnownGitHubs.BranchName);

            // optimize images
            var imagePaths = ImageQuery.FindImages(parameters.LocalPath, repoConfiguration);
            var optimizedImages = OptimizeImages(repo, parameters.LocalPath, imagePaths);
            if (optimizedImages.Length == 0)
                return false;

            // create commit message based on optimizations
            var commitMessage = CommitMessage.Create(optimizedImages);

            // commit
            var signature = new Signature(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail, DateTimeOffset.Now);
            repo.Commit(commitMessage, signature, signature);

            // push to GitHub
            repo.Network.Push(remote, $"refs/heads/{KnownGitHubs.BranchName}", new PushOptions
            {
                CredentialsProvider = credentialsProvider,
            });

            return true;
        }

        private static CompressionResult[] OptimizeImages(Repository repo, string localPath, string[] imagePaths)
        {
            var optimizedImages = new List<CompressionResult>();

            ImageOptimizer imageOptimizer = new ImageOptimizer
            {
                OptimalCompression = true
            };

            Parallel.ForEach(imagePaths, image =>
            {
                try
                {
                    Console.WriteLine(image);
                    FileInfo file = new FileInfo(image);
                    double before = file.Length;
                    if (imageOptimizer.LosslessCompress(file))
                    {
                        optimizedImages.Add(new CompressionResult
                        {
                            Title = image.Substring(localPath.Length),
                            SizeBefore = before / 1024d,
                            SizeAfter = file.Length / 1024d,
                        });

                        Commands.Stage(repo, image);
                    }
                }
                catch
                {
                }
            });

            return optimizedImages.ToArray();
        }
    }
}
