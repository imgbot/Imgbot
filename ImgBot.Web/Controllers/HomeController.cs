using System.Linq;
using ImgBot.Web.Models.Docs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

using static System.IO.File;

namespace ImgBot.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHostingEnvironment _env;

        public HomeController(IHostingEnvironment env)
        {
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult IncidentResponse()
        {
            return View();
        }

        [Route("docs")]
        public IActionResult Docs()
        {
            var docsMetadata = JsonConvert.DeserializeObject<DocSection[]>(ReadAllText($"{_env.ContentRootPath}/Docs/metadata.json"));

            var docs = new DocsViewModel
            {
                DocSections = docsMetadata.Select(doc =>
                {
                    doc.Html = ReadAllText($"{_env.ContentRootPath}/Docs/{doc.Slug}.html");
                    return doc;
                }).ToArray()
            };

            return View(docs);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
