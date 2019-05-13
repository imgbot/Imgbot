using System;
using System.Linq;
using Common;
using CompressImagesFunction.Compress;
using LibGit2Sharp;

namespace CompressImagesFunction.Commits
{
    public static class CommitChanges
    {
        public static void DoCommit(Repository repo, CompressionResult[] optimizedImages, string pgpPrivateKey, string pgPPassword)
        {
              // git add <path to image>
              foreach (var image in optimizedImages)
              {
                  Commands.Stage(repo, image.OriginalPath);
              }

              var commitMessage = CommitMessage.Create(optimizedImages);
              var signature = new Signature(KnownGitHubs.ImgBotLogin, KnownGitHubs.ImgBotEmail, DateTimeOffset.Now);
              repo.Commit(commitMessage, signature, signature);

              // We just made a normal commit, now we are going to capture all the values generated from that commit
              // then rewind and make a signed commit
              var commitBuffer = LibGit2Sharp.Commit.CreateBuffer(
                  repo.Head.Tip.Author,
                  repo.Head.Tip.Committer,
                  repo.Head.Tip.Message,
                  repo.Head.Tip.Tree,
                  repo.Head.Tip.Parents,
                  true,
                  null);

              var signedCommitData = CommitSignature.Sign(commitBuffer + "\n", pgpPrivateKey, pgPPassword);

              repo.Reset(ResetMode.Soft, repo.Head.Commits.Skip(1).First().Sha);
              var commitToKeep = repo.ObjectDatabase.CreateCommitWithSignature(commitBuffer, signedCommitData);

              repo.Refs.UpdateTarget(repo.Refs.Head, commitToKeep);
              var branchAgain = Commands.Checkout(repo, KnownGitHubs.BranchName);
              repo.Reset(ResetMode.Hard, commitToKeep.Sha);
        }
    }
}
