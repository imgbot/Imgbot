using System;

namespace Common
{
    public static class KnownEnvironmentVariables
    {
        public static string TMP
        {
            get { return Environment.GetEnvironmentVariable("TMP"); }
        }

        public static string PGP_PRIVATE_KEY
        {
            get { return Environment.GetEnvironmentVariable("PGP_PRIVATE_KEY"); }
        }

        public static string PGP_PASSWORD
        {
            get { return Environment.GetEnvironmentVariable("PGP_PASSWORD"); }
        }

        public static string APP_PRIVATE_KEY
        {
            get { return Environment.GetEnvironmentVariable("APP_PRIVATE_KEY"); }
        }

        public static string AzureWebJobsStorage
        {
            get { return Environment.GetEnvironmentVariable("AzureWebJobsStorage"); }
        }
    }
}
