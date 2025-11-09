// Identity user-uitbreiding met extra schermnaam (DisplayName).
// Erft van IdentityUser zodat het werkt met ASP.NET Core Identity.

using Microsoft.AspNetCore.Identity;

namespace WorkoutCoachV2.Model.Identity
{
    public class AppUser : IdentityUser
    {
        // Optionele naam die je in de UI toont (los van UserName/Email).
        public string? DisplayName { get; set; }
    }
}
