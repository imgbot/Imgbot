using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
    public static class AuthFunction
    {
        private static HttpClient httpClient = new HttpClient();

        [FunctionName("SetupFunction")]
        public static HttpResponseMessage Setup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "setup")]HttpRequestMessage req,
            ExecutionContext executionContext,
            ILogger logger)
        {
            var secrets = Secrets.Get(executionContext);
            var state = Guid.NewGuid();
            var response = req.CreateResponse();
            response.StatusCode = HttpStatusCode.Redirect;
            response.Headers.Add("location", $"https://github.com/login/oauth/authorize?client_id={secrets.ClientId}&redirect_uri={secrets.RedirectUri}&state={state}");
            response.Headers.Add("set-cookie", $"state={state}");
            return response;
        }

        [FunctionName("CallbackFunction")]
        public static async Task<HttpResponseMessage> Callback(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "callback")]HttpRequestMessage req,
            ExecutionContext executionContext,
            ILogger logger)
        {
            try
            {
                var secrets = Secrets.Get(executionContext);
                var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
                var marketplaceTable = storageAccount.CreateCloudTableClient().GetTableReference("marketplace");

                var stateCookie = req.Headers.GetCookies()
                    ?.FirstOrDefault()
                    ?.Cookies.FirstOrDefault(x => x.Name == "state")
                    ?.Value;

                if (string.IsNullOrEmpty(stateCookie))
                {
                    logger.LogError("state cookie is missing");
                    return Winning(req);
                }

                var qs = req.RequestUri.ParseQueryString();
                var stateQuery = qs.Get("state");
                var code = qs.Get("code");

                if (stateQuery != stateCookie)
                {
                    logger.LogError("state mismatch: {StateCookie} !== {StateQuery}", stateCookie, stateQuery);
                    return Winning(req);
                }

                if (string.IsNullOrEmpty(code))
                {
                    logger.LogError("code is missing");
                    return Winning(req);
                }

                var tokenResponse = await httpClient.PostAsJsonAsync("https://github.com/login/oauth/access_token", new
                {
                    client_id = secrets.ClientId,
                    client_secret = secrets.ClientSecret,
                    code = code,
                    redirect_uri = secrets.RedirectUri,
                    state = stateQuery
                });

                var tokenContent = await tokenResponse.Content.ReadAsFormDataAsync();
                if (tokenContent.Get("error") != null)
                {
                    logger.LogError("TokenResponse: " + await tokenResponse.Content.ReadAsStringAsync());
                    return Winning(req);
                }

                var token = tokenContent.Get("access_token");

                var mktplcRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/marketplace_purchases?access_token=" + token);
                mktplcRequest.Headers.Add("User-Agent", "IMGBOT");
                var mktplcResponse = await httpClient.SendAsync(mktplcRequest);

                var planDataJson = await mktplcResponse.Content.ReadAsStringAsync();
                var planData = JsonConvert.DeserializeObject<PlanData[]>(planDataJson);
                foreach (var item in planData)
                {
                    var marketplaceRow = new Marketplace(item.account.id, item.account.login)
                    {
                        AccountType = item.account.type,
                        PlanId = item.plan.id
                    };

                    await marketplaceTable.CreateIfNotExistsAsync();
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(marketplaceRow));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing auth");
            }

            return Winning(req);
        }

        public static HttpResponseMessage Winning(HttpRequestMessage req)
        {
            var response = req.CreateResponse();
            response.StatusCode = HttpStatusCode.Redirect;
            response.Headers.Add("location", $"https://imgbot.net/winning");
            return response;
        }
    }
}
