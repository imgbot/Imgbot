using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using CompressImagesFunction.Compressors;
using ImageMagick;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Azure.WebJobs;
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
            new MozJpegCompress(),
            new GifsicleCompress(),
        };

        public static bool Run(CompressimagesParameters parameters, ICollector<CompressImagesMessage> compressImagesMessages, ILogger logger)
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
            var isWikiCompress = parameters.CloneUrl.EndsWith(".wiki.git");

            // check if we have the branch already or this is empty repo
            try
            {
                if (repo.Network.ListReferences(remote, credentialsProvider).Any() == false)
                {
                    logger.LogInformation("CompressImagesFunction: no references found for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                    return false;
                }

                if (!parameters.IsRebase && repo.Network.ListReferences(remote, credentialsProvider).Any(x => x.CanonicalName == $"refs/heads/{KnownGitHubs.BranchName}"))
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

            // check if we should switch away from the default branch
            if (!isWikiCompress && parameters.Settings != null && !string.IsNullOrEmpty(parameters.Settings.DefaultBranchOverride))
            {
                logger.LogInformation(
                    "CompressImagesFunction: default branch override for {Owner}/{RepoName} is {DefaultBranchOverride}",
                    parameters.RepoOwner,
                    parameters.RepoName,
                    parameters.Settings.DefaultBranchOverride);

                var baseBranch = repo.Branches[$"refs/remotes/origin/{parameters.Settings.DefaultBranchOverride}"];
                if (baseBranch == null)
                {
                    logger.LogWarning(
                        "CompressImagesFunction: default branch ({DefaultBranchOverride}) not found for {Owner}/{RepoName}",
                        parameters.Settings.DefaultBranchOverride,
                        parameters.RepoOwner,
                        parameters.RepoName);
                    return false;
                }

                Commands.Checkout(repo, baseBranch);
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

            // Add new compressMessage if we should compress Wiki
            if (repoConfiguration.CompressWiki && isWikiCompress == false)
            {
                logger.LogInformation("CompressImagesFunction: Adding Wiki image compression to queue for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                compressImagesMessages.Add(new CompressImagesMessage()
                {
                    InstallationId = parameters.CompressImagesMessage.InstallationId,
                    RepoName = parameters.CompressImagesMessage.RepoName,
                    Owner = parameters.RepoOwner,
                    CloneUrl = $"https://github.com/{parameters.RepoOwner}/{parameters.RepoName}.wiki.git"
                });
            }

            if (Schedule.ShouldOptimizeImages(repoConfiguration, repo) == false)
            {
                logger.LogInformation("CompressImagesFunction: skipping optimization due to schedule for {Owner}/{RepoName}", parameters.RepoOwner, parameters.RepoName);
                return false;
            }

            // Should not create branch if we are compressing Wiki or performing rebase
            if (isWikiCompress == false && !parameters.IsRebase)
            {
                // check out the branch
                repo.CreateBranch(KnownGitHubs.BranchName);
                var branch = Commands.Checkout(repo, KnownGitHubs.BranchName);
            }

            // reset any mean files
            repo.Reset(ResetMode.Mixed, repo.Head.Tip);

            // optimize images
            string[] imagePaths;
            List<string> addedOrModifiedImagePaths = new List<string>();
            List<string> deletedImagePaths = new List<string>();
            if (parameters.IsRebase)
            {
                var refspec = string.Format("{0}:{0}", KnownGitHubs.BranchName);
                Commands.Fetch(repo, "origin", new List<string> { refspec }, null, "fetch");

                var diff = repo.Diff.Compare<TreeChanges>(repo.Branches[KnownGitHubs.BranchName].Commits.ElementAt(1).Tree, repo.Head.Tip.Tree);

                if (diff == null)
                {
                    logger.LogInformation("Something went wrong while doing rebase");
                    return false;
                }

                foreach (TreeEntryChanges c in diff)
                {
                    if (KnownImgPatterns.ImgExtensions.Contains(Path.GetExtension(c.Path)))
                    {
                        var path = Path.Combine(parameters.LocalPath, c.Path).Replace("\\", "/");
                        var oldpath = Path.Combine(parameters.LocalPath, c.OldPath).Replace("\\", "/");

                        switch (c.Status)
                        {
                            case ChangeKind.Added:
                            case ChangeKind.Modified:
                                addedOrModifiedImagePaths.Add(path);
                                break;
                            case ChangeKind.Renamed:
                                addedOrModifiedImagePaths.Add(path);
                                deletedImagePaths.Add(oldpath);
                                break;
                            case ChangeKind.Deleted:
                                deletedImagePaths.Add(path);
                                break;
                        }
                    }
                }

                imagePaths = ImageQuery.FilterOutIgnoredFiles(addedOrModifiedImagePaths, repoConfiguration);
            }
            else
            {
                imagePaths = ImageQuery.FindImages(parameters.LocalPath, repoConfiguration);
            }

            var optimizedImages = OptimizeImages(repo, parameters.LocalPath, imagePaths, logger, repoConfiguration.AggressiveCompression);
            if (optimizedImages.Length == 0)
                return false;

            if (!Threshold.MeetsThreshold(repoConfiguration, optimizedImages))
            {
                logger.LogInformation($"Did not meet threshold. {parameters.RepoOwner}/{parameters.RepoName}");
                return false;
            }

            // create commit message based on optimizations
            foreach (var image in optimizedImages)
            {
                Commands.Stage(repo, image.OriginalPath);
            }

            var commitMessage = CommitMessage.Create(optimizedImages);
            var signature = new Signature(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail, DateTimeOffset.Now);
            repo.Commit(commitMessage, signature, signature);

            if (parameters.IsRebase)
            {
                var baseBranch = repo.Head;
                var newCommit = baseBranch.Tip;
                var oldCommit = repo.Branches[KnownGitHubs.BranchName].Tip;

                // we need to reset the default branch so that we can
                // rebase to it later.
                repo.Reset(ResetMode.Hard, repo.Head.Commits.ElementAt(1));

                // checkout to imgbot branch. TODO: remove because this is needed earlier on diff
                Commands.Checkout(repo, KnownGitHubs.BranchName);

                // cherry-pick
                var cherryPickOptions = new CherryPickOptions()
                {
                    MergeFileFavor = MergeFileFavor.Theirs,
                };
                var cherryPickResult = repo.CherryPick(newCommit, signature, cherryPickOptions);

                if (cherryPickResult.Status == CherryPickStatus.Conflicts)
                {
                    var status = repo.RetrieveStatus(new LibGit2Sharp.StatusOptions() { });
                    foreach (var item in status)
                    {
                        if (item.State == FileStatus.Conflicted)
                        {
                            Commands.Stage(repo, item.FilePath);
                        }
                    }

                    repo.Commit(commitMessage, signature, signature);
                }

                // New commit message creation
                var previousCommitResults = CompressionResult.ParseCommitMessage(oldCommit.Message);
                var mergedResults = CompressionResult.Merge(optimizedImages, previousCommitResults);
                var filteredResults = CompressionResult.Filter(mergedResults, deletedImagePaths.ToArray());
                var squashCommitMessage = CommitMessage.Create(filteredResults);

                // squash
                var baseCommit = repo.Head.Commits.ElementAt(2);
                repo.Reset(ResetMode.Soft, baseCommit);
                repo.Commit(squashCommitMessage, signature, signature);

                // rebase
                var rebaseOptions = new RebaseOptions()
                {
                    FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                };

                var rebaseResult = repo.Rebase.Start(repo.Head, baseBranch, null, new Identity(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail), rebaseOptions);

                while (rebaseResult.Status == RebaseStatus.Conflicts)
                {
                    var status = repo.RetrieveStatus(new LibGit2Sharp.StatusOptions() { });
                    foreach (var item in status)
                    {
                        if (item.State == FileStatus.Conflicted)
                        {
                            if (imagePaths.Contains(Path.Combine(parameters.LocalPath, item.FilePath)))
                            {
                                Commands.Stage(repo, item.FilePath);
                            }
                            else
                            {
                                Commands.Remove(repo, item.FilePath);
                            }
                        }
                    }

                    rebaseResult = repo.Rebase.Continue(new Identity(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail), rebaseOptions);
                }
            }

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

            // Should use "master" if we are compressing Wiki
            if (isWikiCompress)
            {
                var branchAgain = Commands.Checkout(repo, "master");
            }
            else
            {
                var branchAgain = Commands.Checkout(repo, KnownGitHubs.BranchName);
            }

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
            if (isWikiCompress)
            {
                repo.Network.Push(remote, "refs/heads/master", new PushOptions
                {
                    CredentialsProvider = credentialsProvider,
                });
            }
            else
            {
                var refs = $"refs/heads/{KnownGitHubs.BranchName}";
                if (parameters.IsRebase)
                    refs = refs.Insert(0, "+");
                logger.LogInformation("refs: {refs}", refs);

                repo.Network.Push(remote, refs, new PushOptions
                {
                    CredentialsProvider = credentialsProvider,
                });
            }

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
                                        optimizer.LosslessCompress(image);
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
