using Microsoft.AspNetCore.Identity;

namespace WorkoutCoachV2.Model.Identity;

public class AppUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
