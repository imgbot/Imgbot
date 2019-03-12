using System;

namespace Common
{
    public static class KnownEnvironmentVariables
    {
        public static string TMP => Environment.GetEnvironmentVariable("TMP");

        public static string PGP_PRIVATE_KEY => Environment.GetEnvironmentVariable("PGP_PRIVATE_KEY");

        public static string PGP_PASSWORD => Environment.GetEnvironmentVariable("PGP_PASSWORD");

        public static string APP_PRIVATE_KEY => Environment.GetEnvironmentVariable("APP_PRIVATE_KEY");

        public static string AzureWebJobsStorage => Environment.GetEnvironmentVariable("AzureWebJobsStorage");
    }
}
