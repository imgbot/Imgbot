using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Auth.Extensions;
using Auth.Model;
using Common.TableModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Auth
{
    public static class InstallationFunction
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("ListInstallationsFunction")]
        public static async Task<HttpResponseMessage> ListAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "installations")]HttpRequestMessage req,
            ExecutionContext executionContext)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var installationsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/installations?access_token=" + token);
            installationsRequest.Headers.Add("User-Agent", "IMGBOT");
            installationsRequest.Headers.Add("Accept", "application/vnd.github.machine-man-preview+json");
            var installationsResponse = await httpClient.SendAsync(installationsRequest);
            var installationsJson = await installationsResponse.Content.ReadAsStringAsync();
            var installationsData = JsonConvert.DeserializeObject<Installations>(installationsJson);

            var installations = installationsData.installations.Select(x => new
            {
                x.id,
                x.html_url,
            });

            var response = req.CreateResponse();
            response
              .SetJson(new { installations })
              .EnableCors();
            return response;
        }
    }
}
