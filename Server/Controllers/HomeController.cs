using Microsoft.AspNetCore.Mvc;

namespace VentyTime.Server.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        [Route("/")]
        public IActionResult Index()
        {
            return File("index.html", "text/html");
        }
    }
}
