using System.Collections.Generic;

namespace WorkoutCoachV2.Web.Models
{
    // Model voor één rij in de admin-tabel: basisinfo + rollen + blocked status.
    public class AdminUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public bool IsBlocked { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    // Model voor de admin index pagina: lijst van alle gebruikers-rijen.
    public class AdminUsersIndexViewModel
    {
        public List<AdminUserRowViewModel> Users { get; set; } = new();
    }
}
