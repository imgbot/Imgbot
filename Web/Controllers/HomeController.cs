using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Web.Models;
using Web.Models.Docs;

using static System.IO.File;

namespace Web.Controllers
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

        public IActionResult VulnerabilityManagement()
        {
            return View();
        }

        public IActionResult Terms()
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

        [Route("winning")]
        public IActionResult Winning()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
