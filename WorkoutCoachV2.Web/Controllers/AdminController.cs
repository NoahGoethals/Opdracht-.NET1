using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")] 
    public class AdminController : Controller
    {
        private const string RoleAdmin = "Admin";
        private const string RoleModerator = "Moderator";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var vm = new AdminUsersIndexViewModel();

            foreach (var u in users)
            {
                var roles = (await _userManager.GetRolesAsync(u)).ToList();

                vm.Users.Add(new AdminUserRowViewModel
                {
                    Id = u.Id,
                    Email = u.Email ?? "",
                    DisplayName = u.DisplayName ?? "",
                    IsBlocked = u.IsBlocked,
                    Roles = roles
                });
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var meId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            {
                TempData["Error"] = "Je kan jezelf niet blokkeren.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Gebruiker niet gevonden.";
                return RedirectToAction(nameof(Index));
            }

            user.IsBlocked = !user.IsBlocked;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = user.IsBlocked
                ? $"Gebruiker '{user.Email}' is geblokkeerd."
                : $"Gebruiker '{user.Email}' is gedeblokkeerd.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAdminRole(string id)
        {
            return await ToggleRoleInternal(id, RoleAdmin, "Admin");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleModeratorRole(string id)
        {
            return await ToggleRoleInternal(id, RoleModerator, "Moderator");
        }


        private async Task<IActionResult> ToggleRoleInternal(string id, string roleName, string displayRoleName)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var meId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            {
                TempData["Error"] = $"Je kan je eigen {displayRoleName}-role niet via deze knop aanpassen.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                if (!createRoleResult.Succeeded)
                {
                    TempData["Error"] = string.Join(" | ", createRoleResult.Errors.Select(e => e.Description));
                    return RedirectToAction(nameof(Index));
                }
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Gebruiker niet gevonden.";
                return RedirectToAction(nameof(Index));
            }

            var hasRole = await _userManager.IsInRoleAsync(user, roleName);

            IdentityResult result;
            if (hasRole)
                result = await _userManager.RemoveFromRoleAsync(user, roleName);
            else
                result = await _userManager.AddToRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = hasRole
                ? $"{displayRoleName}-role verwijderd van '{user.Email}'."
                : $"{displayRoleName}-role toegekend aan '{user.Email}'.";

            return RedirectToAction(nameof(Index));
        }
    }
}
