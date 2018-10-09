using Newtonsoft.Json;

namespace Install
{
    public class InstallationToken
    {
        public string Token { get; set; }

        [JsonProperty("expires_at")]
        public string ExpiresAt { get; set; }
    }
}
