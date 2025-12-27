using System;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace WorkoutCoachV2.Web.Controllers
{
    // Zet de gekozen taal (culture) in een cookie en stuurt terug naar de vorige pagina.
    public class LanguageController : Controller
    {
        [HttpGet]
        public IActionResult Set(string culture, string returnUrl = "/")
        {
            if (string.IsNullOrWhiteSpace(culture))
                culture = "nl"; // Fallback: standaard Nederlands.

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName, // Standaard cookie naam van ASP.NET localization.
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)), // Maakt cookie waarde zoals "c=nl|uic=nl".
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1), // Taalkeuze 1 jaar bewaren.
                    IsEssential = true // Cookie mag ook zonder consent (essentieel).
                }
            );

            if (!Url.IsLocalUrl(returnUrl))
                returnUrl = "/"; // Beveiliging: geen redirects naar externe sites.

            return LocalRedirect(returnUrl); // Terug naar de originele pagina.
        }
    }
}
