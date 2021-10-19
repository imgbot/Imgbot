using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Auth.Extensions;
using Common;
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
            var installationsData = await GetInstallationsData(token);
            var installations = await Task.WhenAll(installationsData.installations.Select(async x =>
            {
                var mktplc = await marketplaceTable.ExecuteAsync(
                    TableOperation.Retrieve<Common.TableModels.Marketplace>(x.account.id.ToString(), x.account.login));

                return new
                {
                    x.id,
                    x.html_url,
                    x.account.login,
                    accountid = x.account.id,
                    accounttype = x.account.type,
                    x.account.avatar_url,
                    planId = (mktplc.Result as Common.TableModels.Marketplace)?.PlanId,
                    student = (mktplc.Result as Common.TableModels.Marketplace)?.Student,
                    allowedPrivate = (mktplc.Result as Common.TableModels.Marketplace)?.AllowedPrivate,
                    usedPrivate = (mktplc.Result as Common.TableModels.Marketplace)?.UsedPrivate,
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
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/{installationid}/{page:int}")]HttpRequestMessage req,
            string installationid,
            int page)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var installationTable = GetTable("installation");
            var url = $"https://api.github.com/user/installations/{installationid}/repositories";
            if (page > 1)
            {
                url += "?page=" + page;
            }

            var repositoriesRequest = new HttpRequestMessage(HttpMethod.Get, url);
            repositoriesRequest.Headers.Add("Authorization", "token " + token);
            repositoriesRequest.AddGithubHeaders();
            var repositoriesResponse = await HttpClient.SendAsync(repositoriesRequest);
            var repositoriesJson = await repositoriesResponse.Content.ReadAsStringAsync();
            var repositoriesData = JsonConvert.DeserializeObject<Model.Repositories>(repositoriesJson);

            var repositories = await Task.WhenAll(repositoriesData.repositories.Select(x =>
                RepositoryResponse(x, installationTable, installationid)));

            var next = ParseNextHeader(repositoriesResponse);
            var response = req.CreateResponse();
            response
              .SetJson(new { repositories, next })
              .EnableCors();
            return response;
        }

        [FunctionName("RepositoryFunction")]
        public static async Task<HttpResponseMessage> RepositoryAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/{installationid}/repository/{repositoryid}")]HttpRequestMessage req,
            string installationid,
            string repositoryid)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var installationTable = GetTable("installation");
            var repository = await GetRepository(installationid, token, repositoryid).ConfigureAwait(false);
            if (repository == null)
            {
                throw new Exception("repository request mismatch");
            }

            var response = req.CreateResponse();
            response
              .SetJson(new
              {
                  repository = await RepositoryResponse(repository, installationTable, installationid).ConfigureAwait(false)
              })
              .EnableCors();
            return response;
        }

        [FunctionName("ListPullsFunction")]
        public static async Task<HttpResponseMessage> ListPullsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pulls/{login}")] HttpRequestMessage req,
            string login)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var pullsTable = GetTable("pull");
            var installationsData = await GetInstallationsData(token);
            if (!installationsData.installations.Select(x => x.account.login).Contains(login))
            {
                throw new Exception("login request mismatch");
            }

            var query = new TableQuery<Common.TableModels.Pr>().Where($"PartitionKey eq '{login}'");
            var pulls = await pullsTable.ExecuteQuerySegmentedAsync(query, null);
            var response = req.CreateResponse();
            response.SetJson(new
            {
                pulls = pulls.Results.Select(x =>
                {
                    return new
                    {
                        x.Id,
                        x.NumImages,
                        x.Number,
                        x.Owner,
                        x.PercentReduced,
                        x.RepoName,
                        x.SizeAfter,
                        x.SizeBefore,
                        x.SpaceReduced,
                        x.Timestamp
                    };
                }).ToArray()
            }).EnableCors();
            return response;
        }

        [FunctionName("RequestRepositoryCheckFunction")]
        public static async Task<HttpResponseMessage> RequestRepositoryCheckAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/check/{installationid}/{repositoryid}/{compress?}")]HttpRequestMessage req,
            string installationid,
            string repositoryid,
            string compress)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var routerQueue = GetQueue("routermessage");

            var repository = await GetRepository(installationid, token, repositoryid);

            if (repository == null)
            {
                throw new Exception("repository request mismatch");
            }

            var response = req.CreateResponse();
            response.EnableCors();

            var imgbotBranch = await GetImgbotBranch(repository, token);
            if (imgbotBranch != null && compress == null)
            {
                // branch already exists
                response.SetJson(new { status = "branchexists" });
                return response;
            }

            var shouldCompress = true;
            if (compress == "false")
            {
                shouldCompress = false;
            }

            bool updateValid = true;
            int? usedPrivateValue = 0;
            if (compress != null && repository.@private == true)
            {
                updateValid = false;
                var marketplaceTable = GetTable("marketplace");
                var installationsData = await GetInstallationsData(token);
                await Task.WhenAll(installationsData.installations.Select(async x =>
                {
                    var mktplc = await marketplaceTable.ExecuteAsync(
                        TableOperation.Retrieve<Common.TableModels.Marketplace>(x.account.id.ToString(), x.account.login));

                    var allowedPrivate = (mktplc.Result as Common.TableModels.Marketplace)?.AllowedPrivate;
                    var usedPrivate = (mktplc.Result as Common.TableModels.Marketplace)?.UsedPrivate;
                    usedPrivateValue = usedPrivate;
                    if (shouldCompress)
                    {
                        if (usedPrivate < allowedPrivate || allowedPrivate == null)
                        {
                            usedPrivate++;
                            updateValid = true;
                        }
                    }
                    else
                    {
                        usedPrivate--;
                        updateValid = true;
                    }

                    if (updateValid == true)
                    {
                        await marketplaceTable.ExecuteAsync(TableOperation.InsertOrMerge(new Common.TableModels.Marketplace(x.account.id, x.account.login)
                        {
                            UsedPrivate = usedPrivate,
                        }));
                        usedPrivateValue = usedPrivate;
                    }
                }));
            }

            await routerQueue.AddMessageAsync(new CloudQueueMessage(JsonConvert.SerializeObject(new Common.Messages.RouterMessage
            {
                CloneUrl = repository.html_url,
                InstallationId = Convert.ToInt32(installationid),
                Owner = repository.owner.login,
                RepoName = repository.name,
                Compress = shouldCompress,
                IsPrivate = repository.@private,
                Update = updateValid,
            })));

            response.SetJson(new { status = "OK", usedPrivate = usedPrivateValue });
            return response;
        }

        [FunctionName("GetRepositorySettingsFunction")]
        public static async Task<HttpResponseMessage> GetRepositorySettingsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "repositories/settings/{installationid}/{repositoryid}")]HttpRequestMessage req,
            string installationid,
            string repositoryid)
        {
            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var repository = await GetRepository(installationid, token, repositoryid);

            if (repository == null)
            {
                throw new Exception("repository request mismatch");
            }

            var settingsTable = GetTable("settings");
            var settings = await Common.TableModels.SettingsHelper.GetSettings(settingsTable, installationid, repository.name);
            var response = req.CreateResponse();
            if (settings != null)
            {
                response.SetJson(new
                {
                    settings.InstallationId,
                    settings.RepoName,
                    settings.DefaultBranchOverride,
                });
            }

            response.EnableCors();
            return response;
        }

        [FunctionName("SetRepositorySettingsFunction")]
        public static async Task<HttpResponseMessage> SetRepositorySettingsAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "repositories/settings/{installationid}/{repositoryid}")]HttpRequestMessage req,
            string installationid,
            string repositoryid)
        {
            if (req.Method == HttpMethod.Options)
            {
                return req.CreateOptionsResponse();
            }

            var token = req.ReadCookie("token");
            if (token == null)
            {
                throw new Exception("missing authentication");
            }

            var repository = await GetRepository(installationid, token, repositoryid);
            if (repository == null)
            {
                throw new Exception("repository request mismatch");
            }

            var settingsTable = GetTable("settings");
            var settings = await Common.TableModels.SettingsHelper.GetSettings(settingsTable, installationid, repository.name);
            if (settings == null)
            {
                settings = new Common.TableModels.Settings(installationid, repository.name);
            }

            var bodyJson = await req.Content.ReadAsStringAsync();
            var newSettings = JsonConvert.DeserializeObject<Common.TableModels.Settings>(bodyJson);

            settings.DefaultBranchOverride = newSettings.DefaultBranchOverride;

            await settingsTable.ExecuteAsync(TableOperation.InsertOrReplace(settings));
            var response = req.CreateResponse();
            response.EnableCors();
            return response;
        }

        private static CloudTable GetTable(string tableName)
        {
            var storageAccount = CloudStorageAccount.Parse(Common.KnownEnvironmentVariables.AzureWebJobsStorage);
            return storageAccount.CreateCloudTableClient().GetTableReference(tableName);
        }

        private static CloudQueue GetQueue(string queueName)
        {
            var storageAccount = CloudStorageAccount.Parse(Common.KnownEnvironmentVariables.AzureWebJobsStorage);
            return storageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
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
                lastchecked = (installation.Result as Common.TableModels.Installation)?.LastChecked,
                IsPrivate = (installation.Result as Common.TableModels.Installation)?.IsPrivate,
                IsOptimized = (installation.Result as Common.TableModels.Installation)?.IsOptimized
            };
        }

        private static string ParseNextHeader(HttpResponseMessage response)
        {
            var linkHeader = response.Headers
                .FirstOrDefault(x => x.Key == "Link")
                .Value?.FirstOrDefault();

            if (linkHeader != null)
            {
                var match = Regex.Match(linkHeader, @"<https:\/\/api\.github\.com\/user\/installations\/.*\/repositories\?page=(.*)>; rel=""next""");
                if (match.Success)
                {
                    return match.Groups[1].Value as string;
                }
            }

            return null;
        }

        private static async Task<Model.Installations> GetInstallationsData(string token)
        {
            var installationsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/installations");
            installationsRequest.Headers.Authorization = new AuthenticationHeaderValue("token", token);
            installationsRequest.AddGithubHeaders();
            var installationsResponse = await HttpClient.SendAsync(installationsRequest);
            var installationsJson = await installationsResponse.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Model.Installations>(installationsJson);
        }

        private static async Task<Model.Repository> GetRepository(string installationid, string token, string repositoryid)
        {
            Model.Repository repository = null;
            string next = null;

            do
            {
                var url = $"https://api.github.com/user/installations/{installationid}/repositories";
                if (next != null)
                {
                    url += "?page=" + next;
                }

                var repositoriesRequest = new HttpRequestMessage(HttpMethod.Get, url);
                repositoriesRequest.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                repositoriesRequest.AddGithubHeaders();
                var repositoriesResponse = await HttpClient.SendAsync(repositoriesRequest);
                next = ParseNextHeader(repositoriesResponse);
                var repositoriesJson = await repositoriesResponse.Content.ReadAsStringAsync();
                var repositoriesData = JsonConvert.DeserializeObject<Model.Repositories>(repositoriesJson);
                repository = repositoriesData.repositories.FirstOrDefault(x => x.id.ToString() == repositoryid);
            }
            while (repository == null && next != null);

            return repository;
        }

        private static async Task<Model.Branch> GetImgbotBranch(Model.Repository repository, string token)
        {
            Model.Branch imgbotBranch = null;

            try
            {
                var imgbotBranchRequest = new HttpRequestMessage(HttpMethod.Get, repository.branches_url.Replace("{/branch}", $"/{KnownGitHubs.BranchName}"));
                imgbotBranchRequest.Headers.Authorization = new AuthenticationHeaderValue("token", token);
                imgbotBranchRequest.AddGithubHeaders();
                var imgbotBranchResponse = await HttpClient.SendAsync(imgbotBranchRequest);
                var imbotBranchJson = await imgbotBranchResponse.Content.ReadAsStringAsync();
                imgbotBranch = JsonConvert.DeserializeObject<Model.Branch>(imbotBranchJson);
                if (imgbotBranch.name != KnownGitHubs.BranchName)
                {
                    return null;
                }
            }
            catch
            {
            }

            return imgbotBranch;
        }
    }
}
