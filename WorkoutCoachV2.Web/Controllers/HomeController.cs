using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    // Basis controller voor publieke pagina’s: home, privacy en errorpagina.
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger; // Logging voor algemene fouten.

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger; // DI logger.
        }

        public IActionResult Index()
        {
            return View(); // Startpagina.
        }

        public IActionResult Privacy()
        {
            return View(); // Privacy pagina.
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier }); // Toont error met request id.
        }
    }
}
