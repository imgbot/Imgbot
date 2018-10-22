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
    }
}
