using System.IO;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;

namespace Auth
{
    public class Secrets
    {
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }

        public string RedirectUri { get; set; }

        public static Secrets Get(ExecutionContext context)
        {
            return JsonConvert.DeserializeObject<Secrets>(
                File.ReadAllText(Path.Combine(context.FunctionDirectory, $"../secrets.json")));
        }
    }
}
