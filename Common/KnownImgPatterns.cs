using System.Collections.Generic;

namespace Common
{
    public static class KnownImgPatterns
    {
        public static readonly string[] ImgExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".svg" };

        public static readonly Dictionary<string, string> MimeMap = new Dictionary<string, string>
        {
            [".png"] = "image/png",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".gif"] = "image/gif",
            [".svg"] = "image/svg+xml",
        };
    }
}
