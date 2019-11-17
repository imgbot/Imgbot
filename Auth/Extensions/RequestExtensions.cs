using System.Linq;
using System.Net;
using System.Net.Http;

namespace Auth.Extensions
{
    public static class RequestExtensions
    {
        public static string ReadCookie(this HttpRequestMessage req, string name)
        {
            return req.Headers.GetCookies()
                    ?.FirstOrDefault()
                    ?.Cookies.FirstOrDefault(x => x.Name == name)
                    ?.Value;
        }

        public static HttpRequestMessage AddGithubHeaders(this HttpRequestMessage req)
        {
            req.Headers.Add("User-Agent", "IMGBOT");
            req.Headers.Add("Accept", "application/vnd.github.machine-man-preview+json");
            return req;
        }

        public static HttpResponseMessage CreateOptionsResponse(this HttpRequestMessage req)
        {
            var response = req.CreateResponse();
            response.EnableCors();
            response.StatusCode = HttpStatusCode.NoContent;
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            return response;
        }
    }
}
