using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin/Index faalde. AdminUserId={AdminUserId}", _userManager.GetUserId(User));
                TempData["Error"] = "Er ging iets mis bij het laden van de gebruikerslijst.";
                return View(new AdminUsersIndexViewModel());
            }
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
                _logger.LogWarning("Admin probeerde zichzelf te blokkeren. AdminUserId={AdminUserId}", meId);
                TempData["Error"] = "Je kan jezelf niet blokkeren.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("ToggleBlock: gebruiker niet gevonden. TargetUserId={TargetUserId} AdminUserId={AdminUserId}", id, meId);
                TempData["Error"] = "Gebruiker niet gevonden.";
                return RedirectToAction(nameof(Index));
            }

            user.IsBlocked = !user.IsBlocked;

            try
            {
                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var msg = string.Join(" | ", result.Errors.Select(e => e.Description));
                    _logger.LogError("ToggleBlock: UpdateAsync faalde. TargetUserId={TargetUserId} TargetEmail={TargetEmail} Errors={Errors} AdminUserId={AdminUserId}",
                        user.Id, user.Email, msg, meId);

                    TempData["Error"] = msg;
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("ToggleBlock: user status gewijzigd. TargetUserId={TargetUserId} TargetEmail={TargetEmail} IsBlocked={IsBlocked} AdminUserId={AdminUserId}",
                    user.Id, user.Email, user.IsBlocked, meId);

                TempData["Success"] = user.IsBlocked
                    ? $"Gebruiker '{user.Email}' is geblokkeerd."
                    : $"Gebruiker '{user.Email}' is gedeblokkeerd.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleBlock exception. TargetUserId={TargetUserId} TargetEmail={TargetEmail} AdminUserId={AdminUserId}",
                    user.Id, user.Email, meId);

                TempData["Error"] = "Er ging iets mis bij het blokkeren/deblokkeren.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ToggleAdminRole(string id)
            => ToggleRole(id, "Admin");

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ToggleModeratorRole(string id)
            => ToggleRole(id, "Moderator");

        private async Task<IActionResult> ToggleRole(string id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var meId = _userManager.GetUserId(User);
            if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            {
                _logger.LogWarning("Role toggle geblokkeerd: admin probeerde eigen rol aan te passen via knop. Role={Role} AdminUserId={AdminUserId}", roleName, meId);
                TempData["Error"] = "Je kan je eigen rol niet via deze knop aanpassen.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));
                    if (!createRoleResult.Succeeded)
                    {
                        var msg = string.Join(" | ", createRoleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Role '{Role}' aanmaken faalde. Errors={Errors} AdminUserId={AdminUserId}", roleName, msg, meId);
                        TempData["Error"] = msg;
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation("Role '{Role}' aangemaakt.", roleName);
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("ToggleRole: gebruiker niet gevonden. Role={Role} TargetUserId={TargetUserId} AdminUserId={AdminUserId}", roleName, id, meId);
                    TempData["Error"] = "Gebruiker niet gevonden.";
                    return RedirectToAction(nameof(Index));
                }

                var hasRole = await _userManager.IsInRoleAsync(user, roleName);
                IdentityResult result = hasRole
                    ? await _userManager.RemoveFromRoleAsync(user, roleName)
                    : await _userManager.AddToRoleAsync(user, roleName);

                if (!result.Succeeded)
                {
                    var msg = string.Join(" | ", result.Errors.Select(e => e.Description));
                    _logger.LogError("ToggleRole faalde. Role={Role} TargetUserId={TargetUserId} TargetEmail={TargetEmail} Errors={Errors} AdminUserId={AdminUserId}",
                        roleName, user.Id, user.Email, msg, meId);

                    TempData["Error"] = msg;
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("ToggleRole: rol aangepast. Role={Role} TargetUserId={TargetUserId} TargetEmail={TargetEmail} NowHasRole={NowHasRole} AdminUserId={AdminUserId}",
                    roleName, user.Id, user.Email, !hasRole, meId);

                TempData["Success"] = hasRole
                    ? $"{roleName}-role verwijderd van '{user.Email}'."
                    : $"{roleName}-role toegekend aan '{user.Email}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleRole exception. Role={Role} TargetUserId={TargetUserId} AdminUserId={AdminUserId}", roleName, id, meId);
                TempData["Error"] = "Er ging iets mis bij het aanpassen van de rol.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
