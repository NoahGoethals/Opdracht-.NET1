// AuthService: eenvoudige login/registratie + rolstatusbeheer (gebruikt ASP.NET Identity).
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.Services
{
    public class AuthService
    {
        // Huidige identity-user + lijst van rollen.
        private readonly UserManager<ApplicationUser> _userManager;

        public ApplicationUser? CurrentUser { get; private set; }
        public string[] Roles { get; private set; } = Array.Empty<string>();

        // Constructor: injecteert UserManager.
        public AuthService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        // Login met username of e-mail + wachtwoord.
        // Zet CurrentUser en Roles bij succes.
        public async Task<bool> LoginAsync(string userNameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(userNameOrEmail) || string.IsNullOrWhiteSpace(password))
                return false;

            var user =
                await _userManager.FindByNameAsync(userNameOrEmail)
                ?? await _userManager.FindByEmailAsync(userNameOrEmail);

            if (user is null) return false;
            if (user.IsBlocked) return false;

            var ok = await _userManager.CheckPasswordAsync(user, password);
            if (!ok) return false;

            CurrentUser = user;
            Roles = (await _userManager.GetRolesAsync(user)).ToArray();
            return true;
        }

        // Logout: reset state in geheugen.
        public void Logout()
        {
            CurrentUser = null;
            Roles = Array.Empty<string>();
        }

        // Register: maakt nieuwe user aan en zet standaardrol ("User").
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

        // Sneltest: is huidige user in een rol?
        public bool IsInRole(string role) => Roles.Contains(role);
    }
}
