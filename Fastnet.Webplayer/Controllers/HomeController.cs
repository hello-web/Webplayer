using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fastnet.Core;
using Fastnet.Music.Core;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fastnet.Webplayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger log;
        private readonly MusicConfiguration config;
        public HomeController(ILogger<HomeController> logger, IOptions<MusicConfiguration> config)
        {
            this.log = logger;
            this.config = config.Value;
        }
        public IActionResult Index()
        {
            log.Information($"{this.HttpContext.Request.GetDisplayUrl()}");
            //CheckCurrentPort();
            return View();
        }

        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
        private void CheckCurrentPort()
        {
            var requestPort = this.HttpContext.Request.Host.Port ?? 80;
            if (requestPort != config.WebplayerPort)
            {
                log.Warning($"Unexpected port: configured port is {config.WebplayerPort}, found {requestPort}, communication with the music server will not work as designed");
            }
        }
    }
}
