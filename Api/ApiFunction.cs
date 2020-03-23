using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Common;
using Common.Compressors;
using Common.TableModels;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace Api
{
    public static class ApiFunction
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private static ICompress[] optimizers = new ICompress[]
        {
            new ImageMagickCompress(),
            new SvgoCompress(),
            new MozJpegCompress(),
        };

        [FunctionName("TestFunction")]
        public static async Task<HttpResponseMessage> Test(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "test")]HttpRequestMessage req,
            ExecutionContext executionContext,
            ILogger logger)
        {
            var response = req.CreateResponse();
            var x = await ReceiveImage(req, logger);
            return x;
        }

        private static async Task<HttpResponseMessage> ReceiveImage(HttpRequestMessage req, ILogger logger)
        {
            var provider = await req.Content.ReadAsMultipartAsync();
            var httpContent = provider.Contents[0];
            var originalFileName = httpContent.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
            var extension = Path.GetExtension(originalFileName);
            var tempFilePath = $"{Guid.NewGuid().ToString("n").Substring(0, 12)}{extension}";
            using (var inputStream = await httpContent.ReadAsStreamAsync())
            {
                using (var fileStream = File.Create(tempFilePath))
                {
                    inputStream.CopyTo(fileStream);
                }
            }

            var originalSize = File.ReadAllBytes(tempFilePath).Length;

            foreach (var optimizer in optimizers.Where(x => x.SupportedExtensions.Contains(extension)))
            {
                try
                {
                    optimizer.LosslessCompress(tempFilePath);
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Error optimizing with {optimizer.GetType()}");
                }
            }

            var compressedSize = File.ReadAllBytes(tempFilePath).Length;
            var response = req.CreateResponse(HttpStatusCode.OK);
            using (var outputFileStream = new FileStream(tempFilePath, FileMode.Open))
            using (var outputMemoryStream = new MemoryStream())
            {
                await outputFileStream.CopyToAsync(outputMemoryStream);
                response.Content = new ByteArrayContent(outputMemoryStream.ToArray());
                response.Content.Headers.ContentType = new MediaTypeHeaderValue(KnownImgPatterns.MimeMap[extension]);
                response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = originalFileName
                };
            }

            return response;
        }
    }
}
