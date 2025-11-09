// AuthState: eenvoudige read-only snapshot van ingelogde gebruiker + rolhelpers.
using System.Collections.Generic;
using System.Linq;
using WorkoutCoachV2.Model.Identity;

namespace WorkoutCoachV2.App.Services
{
    public class AuthState
    {
        // Ingelogde gebruiker (projectspecifiek AppUser type).
        public AppUser? User { get; set; }

        // Rollen als lijst; readonly voor consumenten.
        public IReadOnlyList<string> Roles { get; set; } = new List<string>();

        // Helpers om snel rollen te checken.
        public bool IsInRole(string role) => Roles.Contains(role);
        public bool IsAdmin => IsInRole("Admin");
        public bool IsCoach => IsInRole("Coach");
        public bool IsMember => IsInRole("Member");
    }
}
