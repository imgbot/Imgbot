using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using CompressImagesFunction.Commits;
using CompressImagesFunction.Find;
using CompressImagesFunction.Repo;
using ImageMagick;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace CompressImagesFunction.Compress
{
    public static class CompressImages
    {
        public static CompressionRunResult Run(CompressimagesParameters parameters, ILogger logger)
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
                {
                    logger.LogInformation("CompressImagesFunction: no references found for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                    return CompressionRunResult.Exit();
                }

                if (repo.Network.ListReferences(remote, credentialsProvider).Any(x => x.CanonicalName == $"refs/heads/{KnownGitHubs.BranchName}"))
                {
                    logger.LogInformation("CompressImagesFunction: branch already exists for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                    return CompressionRunResult.Exit();
                }
            }
            catch (Exception e)
            {
                // log + ignore
                logger.LogWarning(e, "CompressImagesFunction: issue checking for existing branch or empty repo for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
            }

            var repoConfiguration = new RepoConfiguration();

            try
            {
                // see if .imgbotconfig exists in repo root
                var repoConfigJson = File.ReadAllText(parameters.LocalPath + Path.DirectorySeparatorChar + ".imgbotconfig");
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
            {
                logger.LogInformation("CompressImagesFunction: skipping optimization due to schedule for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                return CompressionRunResult.Exit();
            }

            // check out the branch
            repo.CreateBranch(KnownGitHubs.BranchName);
            var branch = Commands.Checkout(repo, KnownGitHubs.BranchName);

            // reset any mean files
            repo.Reset(ResetMode.Mixed, repo.Head.Tip);

            // optimize images
            var imageQueryResult = ImageQuery.FindImages(parameters.LocalPath, repoConfiguration, parameters.UpdatedImages, parameters.Page);
            var optimizedImages = OptimizeImages(repo, parameters.LocalPath, imageQueryResult.ImagePaths, logger, repoConfiguration.AggressiveCompression);
            if (optimizedImages.Length == 0)
            {
                return new CompressionRunResult
                {
                    DidCompress = false,
                    RunNextPage = imageQueryResult.HasMoreImages,
                };
            }

            CommitChanges.DoCommit(repo, optimizedImages, parameters.PgpPrivateKey, parameters.PgPPassword);

            // verify images are not corrupted by reading from git
            // see https://github.com/dabutvin/ImgBot/issues/273
            try
            {
                foreach (var image in optimizedImages)
                {
                    new MagickImage(image.OriginalPath).Dispose();
                }
            }
            catch (MagickErrorException)
            {
                logger.LogError("Corrupt images after reset!");
                return CompressionRunResult.Exit();
            }

            // push to GitHub
            repo.Network.Push(remote, $"refs/heads/{KnownGitHubs.BranchName}", new PushOptions
            {
                CredentialsProvider = credentialsProvider,
            });

            return CompressionRunResult.Success();
        }

        private static CompressionResult[] OptimizeImages(Repository repo, string localPath, string[] imagePaths, ILogger logger, bool aggressiveCompression)
        {
            var optimizedImages = new List<CompressionResult>();
            ImageOptimizer imageOptimizer = new ImageOptimizer
            {
                OptimalCompression = true,
                IgnoreUnsupportedFormats = true,
            };

            Parallel.ForEach(imagePaths, image =>
            {
                try
                {
                    Console.WriteLine(image);
                    FileInfo file = new FileInfo(image);
                    double before = file.Length;
                    if (aggressiveCompression ? imageOptimizer.Compress(file) : imageOptimizer.LosslessCompress(file))
                    {
                        optimizedImages.Add(new CompressionResult
                        {
                            Title = image.Substring(localPath.Length),
                            OriginalPath = image,
                            SizeBefore = before / 1024d,
                            SizeAfter = file.Length / 1024d,
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    logger.LogError(ex, $"Compression issue with {image}");
                }
            });

            logger.LogInformation("Compressed {NumImages}", optimizedImages.Count);
            return optimizedImages.ToArray();
        }
    }
}
