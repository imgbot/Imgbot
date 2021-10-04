using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
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
    public static class AuthFunction
    {
        public static readonly string Webhost = "https://imgbot.net";
        private static readonly HttpClient HttpClient = new HttpClient();

        [FunctionName("SetupFunction")]
        public static HttpResponseMessage Setup(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "setup")]HttpRequestMessage req,
            ExecutionContext executionContext)
        {
            var secrets = Secrets.Get(executionContext);
            var state = Guid.NewGuid().ToString();
            var from = req.RequestUri.ParseQueryString().Get("from");
            if (from == "app")
            {
                state += ",fromapp";
            }

            var response = req.CreateResponse();
            response
                .SetCookie("state", state)
                .SetRedirect($"https://github.com/login/oauth/authorize?client_id={secrets.ClientId}&redirect_uri={secrets.RedirectUri}&state={state}");
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
                var storageAccount = CloudStorageAccount.Parse(Common.KnownEnvironmentVariables.AzureWebJobsStorage);
                var marketplaceTable = storageAccount.CreateCloudTableClient().GetTableReference("marketplace");

                var stateCookie = req.ReadCookie("state");

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

                var tokenResponse = await HttpClient.PostAsJsonAsync("https://github.com/login/oauth/access_token", new
                {
                    client_id = secrets.ClientId,
                    client_secret = secrets.ClientSecret,
                    code,
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

                var mktplcRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/marketplace_purchases");
                mktplcRequest.Headers.Add("User-Agent", "IMGBOT");
                mktplcRequest.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                var mktplcResponse = await HttpClient.SendAsync(mktplcRequest);
                var planDataJson = await mktplcResponse.Content.ReadAsStringAsync();
                var planData = JsonConvert.DeserializeObject<PlanData[]>(planDataJson);
                var eduData = new Edu();
                var isStudent = false;
                try
                {
                    var eduRequest = new HttpRequestMessage(HttpMethod.Get, "https://education.github.com/api/user");
                    eduRequest.Headers.Add("User-Agent", "IMGBOT");
                    eduRequest.Headers.Add("Authorization", "token " + token);
                    var eduResponse = await HttpClient.SendAsync(eduRequest);
                    var eduDataJson = await eduResponse.Content.ReadAsStringAsync();
                    eduData = JsonConvert.DeserializeObject<Edu>(eduDataJson);
                    if (eduData != null)
                    {
                        isStudent = eduData.Student;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error processing auth education");
                }

                foreach (var item in planData)
                {
                    var marketplaceRow = new Marketplace(item.account.id, item.account.login)
                    {
                        AccountType = item.account.type,
                        PlanId = item.plan.id,
                        Student = isStudent,
                    };
                    await marketplaceTable.CreateIfNotExistsAsync();
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(marketplaceRow));
                }

                if (planData.Length == 0 && eduData.Student == true)
                {
                    // no marketplace data so we need to get the account id from the user api
                    var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
                    userRequest.Headers.Add("User-Agent", "IMGBOT");
                    userRequest.Headers.Add("Authorization", "token " + token);
                    var userResponse = await HttpClient.SendAsync(userRequest);
                    var userDataJson = await userResponse.Content.ReadAsStringAsync();
                    var userData = JsonConvert.DeserializeObject<Account>(userDataJson);
                    var marketplaceRow = new Marketplace(userData.id, userData.login)
                    {
                        AccountType = userData.type,
                        PlanId = 1337,
                        Student = eduData.Student,
                    };

                    await marketplaceTable.CreateIfNotExistsAsync();
                    await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(marketplaceRow));
                }

                return Winning(req, token, stateQuery);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error processing auth");
            }

            return Winning(req);
        }

        public static HttpResponseMessage Winning(HttpRequestMessage req, string token = null, string state = null)
        {
            var response = req.CreateResponse();
            if (token != null)
            {
                response.SetCookie("token", token);
            }

            if (state != null && state.Contains(",") && state.Split(',')[1] == "fromapp")
            {
                response.SetRedirect(Webhost + "/app");
            }
            else
            {
                response.SetRedirect(Webhost + "/winning");
            }

            return response;
        }

        [FunctionName("IsAuthenticatedFunction")]
        public static HttpResponseMessage IsAuthenticated(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "isauthenticated")]HttpRequestMessage req)
        {
            var tokenCookie = req.ReadCookie("token");
            var response = req.CreateResponse();
            response.StatusCode = HttpStatusCode.OK;
            response
                .SetJson(new { result = !string.IsNullOrEmpty(tokenCookie) })
                .EnableCors();
            return response;
        }

        [FunctionName("SignoutFunction")]
        public static HttpResponseMessage Signout(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "signout")]HttpRequestMessage req)
        {
            var response = req
                .CreateResponse()
                .SetCookie("token", "rubbish", new DateTime(1970, 1, 1))
                .SetRedirect(Webhost + "/app");
            return response;
        }
    }
}
