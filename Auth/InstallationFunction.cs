using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Auth.Extensions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Auth
{
    public static class InstallationFunction
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [FunctionName("ListInstallationsFunction")]
        public static async Task<HttpResponseMessage> ListAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "installations")]HttpRequestMessage req)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var marketplaceTable = GetTable("marketplace");
            var installationsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/installations?access_token=" + token);
            installationsRequest.AddGithubHeaders();
            var installationsResponse = await HttpClient.SendAsync(installationsRequest);
            var installationsJson = await installationsResponse.Content.ReadAsStringAsync();
            var installationsData = JsonConvert.DeserializeObject<Model.Installations>(installationsJson);

            var installations = await Task.WhenAll(installationsData.installations.Select(async x =>
            {
                var mktplc = await marketplaceTable.ExecuteAsync(
                    TableOperation.Retrieve<Common.TableModels.Marketplace>(x.account.id.ToString(), x.account.login));

                return new
                {
                    x.id,
                    x.html_url,
                    x.account.login,
                    x.account.avatar_url,
                    planId = (mktplc.Result as Common.TableModels.Marketplace)?.PlanId
                };
            }));

            var response = req.CreateResponse();
            response
              .SetJson(new { installations })
              .EnableCors();
            return response;
        }

        [FunctionName("ListInstallationRepositoriesFunction")]
        public static async Task<HttpResponseMessage> ListRepositoriesAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/{installationid}")]HttpRequestMessage req,
            string installationid)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var installationTable = GetTable("installation");
            var repositoriesRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/user/installations/{installationid}/repositories?access_token=" + token);
            repositoriesRequest.AddGithubHeaders();
            var repositoriesResponse = await HttpClient.SendAsync(repositoriesRequest);
            var repositoriesJson = await repositoriesResponse.Content.ReadAsStringAsync();
            var repositoriesData = JsonConvert.DeserializeObject<Model.Repositories>(repositoriesJson);

            var repositories = await Task.WhenAll(repositoriesData.repositories.Select(x =>
                RepositoryResponse(x, installationTable, installationid)));

            var response = req.CreateResponse();
            response
              .SetJson(new { repositories })
              .EnableCors();
            return response;
        }

        [FunctionName("RepositoryFunction")]
        public static async Task<HttpResponseMessage> RepositoryAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/{installationid}/{repositoryid}")]HttpRequestMessage req,
            string installationid,
            string repositoryid)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var installationTable = GetTable("installation");
            var repositoriesRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/user/installations/{installationid}/repositories?access_token=" + token);
            repositoriesRequest.AddGithubHeaders();
            var repositoriesResponse = await HttpClient.SendAsync(repositoriesRequest);
            var repositoriesJson = await repositoriesResponse.Content.ReadAsStringAsync();
            var repositoriesData = JsonConvert.DeserializeObject<Model.Repositories>(repositoriesJson);

            var repository = repositoriesData.repositories.FirstOrDefault(x => x.id.ToString() == repositoryid);

            if (repository == null)
            {
                throw new Exception("repository request mismatch");
            }

            var response = req.CreateResponse();
            response
              .SetJson(new
              {
                  repository = await RepositoryResponse(repository, installationTable, installationid)
              })
              .EnableCors();
            return response;
        }

        [FunctionName("RequestRepositoryCheckFunction")]
        public static async Task<HttpResponseMessage> RequestRepositoryCheckAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/check/{installationid}/{repositoryid}")]HttpRequestMessage req,
            string installationid,
            string repositoryid)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var routerQueue = storageAccount.CreateCloudQueueClient().GetQueueReference("routermessage");
            var repositoriesRequest = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/user/installations/{installationid}/repositories?access_token=" + token);
            repositoriesRequest.Headers.Add("User-Agent", "IMGBOT");
            repositoriesRequest.Headers.Add("Accept", "application/vnd.github.machine-man-preview+json");
            var repositoriesResponse = await HttpClient.SendAsync(repositoriesRequest);
            var repositoriesJson = await repositoriesResponse.Content.ReadAsStringAsync();
            var repositoriesData = JsonConvert.DeserializeObject<Model.Repositories>(repositoriesJson);

            var repository = repositoriesData.repositories.FirstOrDefault(x => x.id.ToString() == repositoryid);

            if (repository == null)
            {
                throw new Exception("repository request mismatch");
            }

            await routerQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new Common.Messages.RouterMessage
            {
                CloneUrl = repository.html_url,
                InstallationId = Convert.ToInt32(installationid),
                Owner = repository.owner.login,
                RepoName = repository.name,
            })));

            var response = req.CreateResponse();
            response
              .SetJson(new { status = "OK" })
              .EnableCors();
            return response;
        }

        private static CloudTable GetTable(string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            return storageAccount.CreateCloudTableClient().GetTableReference(tableName);
        }

        private static async Task<object> RepositoryResponse(Model.Repository ghRepository, CloudTable installationsTable, string installationid)
        {
            var installation = await installationsTable.ExecuteAsync(
                    TableOperation.Retrieve<Common.TableModels.Installation>(installationid, ghRepository.name));

            return new
            {
                ghRepository.id,
                ghRepository.html_url,
                ghRepository.name,
                ghRepository.fork,
                lastchecked = (installation.Result as Common.TableModels.Installation)?.LastChecked
            };
        }
    }
}
