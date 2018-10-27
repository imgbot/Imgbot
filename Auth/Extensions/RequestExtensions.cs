using System.Linq;
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
    }
}
