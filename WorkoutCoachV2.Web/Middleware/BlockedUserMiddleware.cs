using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Middleware
{
    public class BlockedUserMiddleware
    {
        private readonly RequestDelegate _next;

        public BlockedUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<BlockedUserMiddleware> logger)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";

                if (!path.StartsWith("/account") && !path.StartsWith("/language"))
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                    if (!string.IsNullOrWhiteSpace(userId))
                    {
                        var user = await userManager.FindByIdAsync(userId);

                        if (user == null || user.IsBlocked)
                        {
                            logger.LogInformation("BlockedUserMiddleware: user {UserId} is blocked or missing => signing out.", userId);

                            await context.SignOutAsync(IdentityConstants.ApplicationScheme);

                            var returnUrl = context.Request.Path + context.Request.QueryString;
                            var target = "/Account/Login?blocked=1&returnUrl=" + Uri.EscapeDataString(returnUrl);

                            context.Response.Redirect(target);
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
