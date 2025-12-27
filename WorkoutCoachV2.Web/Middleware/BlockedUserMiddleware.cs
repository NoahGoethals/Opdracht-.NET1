using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Middleware
{
    public class BlockedUserMiddleware
    {
        private readonly RequestDelegate _next;

        // Bewaart de volgende stap in de pipeline zodat de request kan doorgaan als de gebruiker niet geblokkeerd is.
        public BlockedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // Controleert bij elke request: als de gebruiker ingelogd is en geblokkeerd is, dan wordt die meteen uitgelogd en geweigerd/omgeleid.
        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<BlockedUserMiddleware> logger)
        {
            // Alleen relevant als er een ingelogde gebruiker is.
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                // Leest het pad (URL) zodat uitzonderingen kunnen worden toegelaten.
                var path = (context.Request.Path.Value ?? string.Empty).ToLowerInvariant();

                // Laat Account en Language altijd door (anders kan login/taalwissel vastlopen).
                if (!path.StartsWith("/account") && !path.StartsWith("/language"))
                {
                    // Haalt het userId uit de claims van de huidige gebruiker.
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        // Haalt de gebruiker opnieuw op uit de database om de laatste “IsBlocked” status te kennen.
                        var user = await userManager.FindByIdAsync(userId);

                        // Als de gebruiker niet bestaat of geblokkeerd is, dan stopt de request hier.
                        if (user == null || user.IsBlocked)
                        {
                            // Logt dit zodat duidelijk is waarom een user ineens wordt uitgelogd.
                            logger.LogInformation(
                                "BlockedUserMiddleware: user {UserId} is blocked or missing. Path={Path}",
                                userId,
                                context.Request.Path.Value);

                            // API-calls krijgen JSON + 403 zodat de app dit kan afhandelen.
                            if (path.StartsWith("/api"))
                            {
                                // Logout zodat tokens/cookies niet blijven “hangen”.
                                await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                                context.Response.ContentType = "application/json";

                                await context.Response.WriteAsync("{\"error\":\"blocked\"}");
                                return;
                            }

                            // Webpagina’s: logout + redirect naar login met returnUrl zodat de user terug kan keren na login.
                            await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                            var returnUrl = context.Request.Path + context.Request.QueryString;
                            var target = "/Account/Login?blocked=1&returnUrl=" + Uri.EscapeDataString(returnUrl);

                            context.Response.Redirect(target);
                            return;
                        }
                    }
                }
            }

            // Als alles ok is, gaat de request gewoon verder naar de volgende middleware/controller.
            await _next(context);
        }
    }
}
