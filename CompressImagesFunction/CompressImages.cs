using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using CompressImagesFunction.Compressors;
using ImageMagick;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CompressImagesFunction
{
    public static class CompressImages
    {
        private static ICompress[] optimizers = new ICompress[]
        {
            new ImageMagickCompress(),
            new SvgoCompress(),
        };

        public static bool Run(CompressimagesParameters parameters, ILogger logger)
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
                    return false;
                }

                if (repo.Network.ListReferences(remote, credentialsProvider).Any(x => x.CanonicalName == $"refs/heads/{KnownGitHubs.BranchName}"))
                {
                    logger.LogInformation("CompressImagesFunction: branch already exists for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                    return false;
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
                return false;
            }

            // check out the branch
            repo.CreateBranch(KnownGitHubs.BranchName);
            var branch = Commands.Checkout(repo, KnownGitHubs.BranchName);

            // reset any mean files
            repo.Reset(ResetMode.Mixed, repo.Head.Tip);

            // optimize images
            var imagePaths = ImageQuery.FindImages(parameters.LocalPath, repoConfiguration);
            var optimizedImages = OptimizeImages(repo, parameters.LocalPath, imagePaths, logger, repoConfiguration.AggressiveCompression);
            if (optimizedImages.Length == 0)
                return false;

            // create commit message based on optimizations
            foreach (var image in optimizedImages)
            {
                Commands.Stage(repo, image.OriginalPath);
            }

            var commitMessage = CommitMessage.Create(optimizedImages);
            var signature = new Signature(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail, DateTimeOffset.Now);
            repo.Commit(commitMessage, signature, signature);

            // We just made a normal commit, now we are going to capture all the values generated from that commit
            // then rewind and make a signed commit
            var commitBuffer = Commit.CreateBuffer(
                repo.Head.Tip.Author,
                repo.Head.Tip.Committer,
                repo.Head.Tip.Message,
                repo.Head.Tip.Tree,
                repo.Head.Tip.Parents,
                true,
                null);

            var signedCommitData = CommitSignature.Sign(commitBuffer + "\n", parameters.PgpPrivateKey, parameters.PgPPassword);

            repo.Reset(ResetMode.Soft, repo.Head.Commits.Skip(1).First().Sha);
            var commitToKeep = repo.ObjectDatabase.CreateCommitWithSignature(commitBuffer, signedCommitData);

            repo.Refs.UpdateTarget(repo.Refs.Head, commitToKeep);
            var branchAgain = Commands.Checkout(repo, KnownGitHubs.BranchName);
            repo.Reset(ResetMode.Hard, commitToKeep.Sha);

            // verify images are not corrupted by reading from git
            // see https://github.com/dabutvin/ImgBot/issues/273
            try
            {
                foreach (var image in optimizedImages)
                {
                    if (image.OriginalPath.EndsWith(".svg"))
                    {
                        // do not use ImageMagick to verify SVGs
                        continue;
                    }

                    new MagickImage(image.OriginalPath).Dispose();
                }
            }
            catch (MagickErrorException)
            {
                logger.LogError("Corrupt images after reset!");
                return false;
            }

            // push to GitHub
            repo.Network.Push(remote, $"refs/heads/{KnownGitHubs.BranchName}", new PushOptions
            {
                CredentialsProvider = credentialsProvider,
            });

            return true;
        }

        private static CompressionResult[] OptimizeImages(Repository repo, string localPath, string[] imagePaths, ILogger logger, bool aggressiveCompression)
        {
            var optimizedImages = new List<CompressionResult>();
            Parallel.ForEach(imagePaths, image =>
            {
                try
                {
                    var tokenSource = new CancellationTokenSource();
                    var task = Task.Factory.StartNew(
                        () =>
                        {
                            Console.WriteLine(image);
                            FileInfo file = new FileInfo(image);
                            double before = file.Length;
                            var extension = Path.GetExtension(image);
                            foreach (var optimizer in optimizers)
                            {
                                if (optimizer.SupportedExtensions.Contains(extension))
                                {
                                    if (aggressiveCompression)
                                    {
                                        optimizer.LossyCompress(image);
                                    }
                                    else
                                    {
                                        optimizer.LosslessCompress(image);
                                    }
                                }
                            }

                            FileInfo fileAfter = new FileInfo(image);
                            if (fileAfter.Length < before && fileAfter.Length > 0)
                            {
                                optimizedImages.Add(new CompressionResult
                                {
                                    Title = image.Substring(localPath.Length),
                                    OriginalPath = image,
                                    SizeBefore = before / 1024d,
                                    SizeAfter = fileAfter.Length / 1024d,
                                });
                            }
                        },
                        tokenSource.Token);

                    // returns true if the Task completed execution within the allotted time; otherwise, false.
                    // Cancel and continue with the rest
                    if (task.Wait(600 * 1000) == false)
                    {
                        logger.LogInformation("Timeout processing {Image}", image);
                        tokenSource.Cancel();
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
