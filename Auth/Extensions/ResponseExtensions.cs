using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Auth.Extensions
{
    public static class ResponseExtensions
    {
        public static HttpResponseMessage SetCookie(this HttpResponseMessage res, string name, string value)
        {
            return res.SetCookie(name, value, DateTime.Now.AddDays(14));
        }

        public static HttpResponseMessage SetCookie(this HttpResponseMessage res, string name, string value, DateTime expiry)
        {
          var expireString = expiry
              .ToUniversalTime()
              .ToString("ddd, dd-MMM-yyyy HH':'mm':'ss 'GMT'", DateTimeFormatInfo.InvariantInfo);

          res.Headers.Add("Set-Cookie", $"{name}={value}; Expires=${expireString}; Secure; HttpOnly");
          return res;
        }

        public static HttpResponseMessage SetRedirect(this HttpResponseMessage res, string location)
        {
            res.Headers.Add("location", location);
            res.StatusCode = HttpStatusCode.Redirect;
            return res;
        }

        public static HttpResponseMessage EnableCors(this HttpResponseMessage res)
        {
            res.Headers.Add("Access-Control-Allow-Credentials", "true");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS, POST");
            res.Headers.Add("Access-Control-Allow-Origin", AuthFunction.Webhost);
            return res;
        }

        public static HttpResponseMessage SetJson(this HttpResponseMessage res, object data)
        {
            res.Content = new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json");
            return res;
        }
    }
}
