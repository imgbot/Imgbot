using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ImgBot.Web.Controllers
{
    public class HookController : Controller
    {
        public IActionResult Index()
        {
            return Json(new { data = true });
        }
    }
}
