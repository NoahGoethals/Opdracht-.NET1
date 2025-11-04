using System.Collections.Generic;
using System.Linq;
using WorkoutCoachV2.Model.Identity;

namespace WorkoutCoachV2.App.Services
{
    public class AuthState
    {
        public AppUser? User { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();

        public bool IsInRole(string role) => Roles.Contains(role);
        public bool IsAdmin => IsInRole("Admin");
        public bool IsCoach => IsInRole("Coach");
        public bool IsMember => IsInRole("Member");
    }
}
