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
    // Admin UI controller: gebruikers bekijken, blokkeren/deblokkeren en rollen beheren.
    [Authorize(Policy = "AdminOnly")] // Alleen admins mogen deze pagina's zien.
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager; // Leest/wijzigt users.
        private readonly RoleManager<IdentityRole> _roleManager; // Leest/maakt rollen.
        private readonly ILogger<AdminController> _logger; // Logging voor fouten en acties.

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _userManager = userManager; // DI user beheer.
            _roleManager = roleManager; // DI role beheer.
            _logger = logger; // DI logging.
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.Email)
                    .ToListAsync(); // Haalt alle users op, alfabetisch.

                var vm = new AdminUsersIndexViewModel(); // ViewModel met tabel-rows.

                foreach (var u in users)
                {
                    var roles = (await _userManager.GetRolesAsync(u)).ToList(); // Rollen per user ophalen.

                    vm.Users.Add(new AdminUserRowViewModel
                    {
                        Id = u.Id,
                        Email = u.Email ?? "",
                        DisplayName = u.DisplayName ?? "",
                        IsBlocked = u.IsBlocked,
                        Roles = roles
                    });
                }

                return View(vm); // Toont admin gebruikerslijst.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Admin/Index faalde. AdminUserId={AdminUserId}", _userManager.GetUserId(User)); // Logt de fout met admin id.
                TempData["Error"] = "Er ging iets mis bij het laden van de gebruikerslijst."; // UI melding.
                return View(new AdminUsersIndexViewModel()); // Lege lijst tonen i.p.v. crash.
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleBlock(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index)); // Geen id => terug naar lijst.

            var meId = _userManager.GetUserId(User); // Huidige admin id.
            if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            {
                _logger.LogWarning("Admin probeerde zichzelf te blokkeren. AdminUserId={AdminUserId}", meId); // Zelf blokkeren mag niet.
                TempData["Error"] = "Je kan jezelf niet blokkeren.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id); // Doel-user ophalen.
            if (user == null)
            {
                _logger.LogWarning("ToggleBlock: gebruiker niet gevonden. TargetUserId={TargetUserId} AdminUserId={AdminUserId}", id, meId); // Log: user bestaat niet.
                TempData["Error"] = "Gebruiker niet gevonden.";
                return RedirectToAction(nameof(Index));
            }

            user.IsBlocked = !user.IsBlocked; // Toggle blocked flag.

            try
            {
                var result = await _userManager.UpdateAsync(user); // Opslaan via Identity.
                if (!result.Succeeded)
                {
                    var msg = string.Join(" | ", result.Errors.Select(e => e.Description)); // Errors bundelen.
                    _logger.LogError("ToggleBlock: UpdateAsync faalde. TargetUserId={TargetUserId} TargetEmail={TargetEmail} Errors={Errors} AdminUserId={AdminUserId}",
                        user.Id, user.Email, msg, meId);

                    TempData["Error"] = msg; // UI foutmelding.
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("ToggleBlock: user status gewijzigd. TargetUserId={TargetUserId} TargetEmail={TargetEmail} IsBlocked={IsBlocked} AdminUserId={AdminUserId}",
                    user.Id, user.Email, user.IsBlocked, meId); // Logt succesvolle actie.

                TempData["Success"] = user.IsBlocked
                    ? $"Gebruiker '{user.Email}' is geblokkeerd."
                    : $"Gebruiker '{user.Email}' is gedeblokkeerd."; // UI feedback.

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleBlock exception. TargetUserId={TargetUserId} TargetEmail={TargetEmail} AdminUserId={AdminUserId}",
                    user.Id, user.Email, meId); // Onverwachte fout loggen.

                TempData["Error"] = "Er ging iets mis bij het blokkeren/deblokkeren.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ToggleAdminRole(string id)
            => ToggleRole(id, "Admin"); // Knop: admin rol geven/wegnemen.

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> ToggleModeratorRole(string id)
            => ToggleRole(id, "Moderator"); // Knop: moderator rol geven/wegnemen.

        private async Task<IActionResult> ToggleRole(string id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index)); // Geen id => terug.

            var meId = _userManager.GetUserId(User); // Huidige admin id.
            if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            {
                _logger.LogWarning("Role toggle geblokkeerd: admin probeerde eigen rol aan te passen via knop. Role={Role} AdminUserId={AdminUserId}", roleName, meId); // Zelf rol aanpassen via knop blokkeren.
                TempData["Error"] = "Je kan je eigen rol niet via deze knop aanpassen.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var createRoleResult = await _roleManager.CreateAsync(new IdentityRole(roleName)); // Rol aanmaken als die nog niet bestaat.
                    if (!createRoleResult.Succeeded)
                    {
                        var msg = string.Join(" | ", createRoleResult.Errors.Select(e => e.Description));
                        _logger.LogError("Role '{Role}' aanmaken faalde. Errors={Errors} AdminUserId={AdminUserId}", roleName, msg, meId);
                        TempData["Error"] = msg;
                        return RedirectToAction(nameof(Index));
                    }

                    _logger.LogInformation("Role '{Role}' aangemaakt.", roleName);
                }

                var user = await _userManager.FindByIdAsync(id); // Doel-user ophalen.
                if (user == null)
                {
                    _logger.LogWarning("ToggleRole: gebruiker niet gevonden. Role={Role} TargetUserId={TargetUserId} AdminUserId={AdminUserId}", roleName, id, meId);
                    TempData["Error"] = "Gebruiker niet gevonden.";
                    return RedirectToAction(nameof(Index));
                }

                var hasRole = await _userManager.IsInRoleAsync(user, roleName); // Check: user heeft rol al?
                IdentityResult result = hasRole
                    ? await _userManager.RemoveFromRoleAsync(user, roleName) // Dan verwijderen.
                    : await _userManager.AddToRoleAsync(user, roleName); // Anders toevoegen.

                if (!result.Succeeded)
                {
                    var msg = string.Join(" | ", result.Errors.Select(e => e.Description));
                    _logger.LogError("ToggleRole faalde. Role={Role} TargetUserId={TargetUserId} TargetEmail={TargetEmail} Errors={Errors} AdminUserId={AdminUserId}",
                        roleName, user.Id, user.Email, msg, meId);

                    TempData["Error"] = msg;
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("ToggleRole: rol aangepast. Role={Role} TargetUserId={TargetUserId} TargetEmail={TargetEmail} NowHasRole={NowHasRole} AdminUserId={AdminUserId}",
                    roleName, user.Id, user.Email, !hasRole, meId); // Logt de wijziging.

                TempData["Success"] = hasRole
                    ? $"{roleName}-role verwijderd van '{user.Email}'."
                    : $"{roleName}-role toegekend aan '{user.Email}'.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ToggleRole exception. Role={Role} TargetUserId={TargetUserId} AdminUserId={AdminUserId}", roleName, id, meId); // Logt onverwachte fout.
                TempData["Error"] = "Er ging iets mis bij het aanpassen van de rol.";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
