using System.Collections.Generic;

namespace WorkoutCoachV2.Web.Models
{
    public class AdminUserRowViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;

        public bool IsBlocked { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class AdminUsersIndexViewModel
    {
        public List<AdminUserRowViewModel> Users { get; set; } = new();
    }
}
