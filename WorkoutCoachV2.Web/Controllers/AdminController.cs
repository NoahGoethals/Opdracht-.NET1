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
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var meId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            {
                TempData["Error"] = "Je kan je eigen Admin-role niet via deze knop aanpassen.";
                return RedirectToAction(nameof(Index));
            }

            if (!await _roleManager.RoleExistsAsync("Admin"))
                await _roleManager.CreateAsync(new IdentityRole("Admin"));

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Gebruiker niet gevonden.";
                return RedirectToAction(nameof(Index));
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            IdentityResult result;

            if (isAdmin)
                result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            else
                result = await _userManager.AddToRoleAsync(user, "Admin");

            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = isAdmin
                ? $"Admin-role verwijderd van '{user.Email}'."
                : $"Admin-role toegekend aan '{user.Email}'.";

            return RedirectToAction(nameof(Index));
        }
    }
}
