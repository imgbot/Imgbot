using System.IO;
using Common.Messages;

namespace CompressImagesFunction.Compress
{
    public class CompressimagesParameters
    {
        public string RepoOwner { get; set; }

        public string RepoName { get; set; }

        public string LocalPath { get; set; }

        public string CloneUrl { get; set; }

        public string Password { get; set; }

        public string PgpPrivateKey { get; set; }

        public string PgPPassword { get; set; }

        public CompressImagesMessage CompressImagesMessage { get; set; }

        public int Page { get; set; } = 1;
  }
}
