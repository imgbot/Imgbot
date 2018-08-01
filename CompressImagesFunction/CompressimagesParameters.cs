using System.IO;

namespace CompressImagesFunction
{
    public class CompressimagesParameters
    {
        public string RepoOwner { get; set; }

        public string RepoName { get; set; }

        public string LocalPath { get; set; }

        public string CloneUrl { get; set; }

        public string Password { get; set; }

        public Stream PgpPrivateKeyStream { get; set; }

        public string PgPPassword { get; set; }
    }
}
