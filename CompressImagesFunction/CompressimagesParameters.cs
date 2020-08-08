using System.IO;
using Common.Messages;

namespace CompressImagesFunction
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

        public bool IsRebase { get; set; }

        public Common.TableModels.Settings Settings { get; set; }

        public CompressImagesMessage CompressImagesMessage { get; set; }
  }
}
