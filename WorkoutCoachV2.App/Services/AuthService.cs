using Microsoft.AspNetCore.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.Services
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationUser? CurrentUser { get; private set; }
        public string[] Roles { get; private set; } = Array.Empty<string>();

        public AuthService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<bool> LoginAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user is null || user.IsBlocked) return false;

            var ok = await _userManager.CheckPasswordAsync(user, password);
            if (!ok) return false;

            CurrentUser = user;
            var roles = await _userManager.GetRolesAsync(user);
            Roles = roles.ToArray();
            return true;
        }

        public void Logout()
        {
            CurrentUser = null;
            Roles = Array.Empty<string>();
        }

        public async Task<IdentityResult> RegisterAsync(string userName, string password, string email, string displayName)
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                DisplayName = displayName
            };
            var res = await _userManager.CreateAsync(user, password);
            if (res.Succeeded)
                await _userManager.AddToRoleAsync(user, "User");
            return res;
        }

        public bool IsInRole(string role) => Roles.Contains(role);
    }
}
