using Microsoft.AspNetCore.Identity;

namespace WorkoutCoachV2.Model.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string DisplayName { get; set; } = "";
        public bool IsBlocked { get; set; } = false;
    }
}
