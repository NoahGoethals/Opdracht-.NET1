using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // API controller met automatische modelbinding/validatie-conventies.
[Route("api/admin")] // Base route voor admin endpoints.
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminOnly")] // Alleen JWT + AdminOnly policy.
public class AdminApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager; // Identity userbeheer (users ophalen/updaten/rollen lezen).
    private readonly RoleManager<IdentityRole> _roleManager; // Identity rolbeheer (controleren of rol bestaat).

    private static readonly string[] AllowedRoles = ["Admin", "Moderator", "User"]; // Whitelist: enkel deze rollen mogen gezet worden.

    public AdminApiController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager; // Dependency injection van UserManager.
        _roleManager = roleManager; // Dependency injection van RoleManager.
    }

    [HttpGet("users")] // GET api/admin/users: lijst van alle users + rollen.
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
    {
        var users = await _userManager.Users // Query over Identity users.
            .OrderBy(u => u.Email) // Sorteren op e-mail voor consistente output.
            .ToListAsync(); // Async ophalen uit DB.

        var result = new List<AdminUserDto>(); // DTO-lijst voor response.

        foreach (var u in users) // Rollen per user moeten afzonderlijk opgehaald worden.
        {
            var roles = (await _userManager.GetRolesAsync(u)).ToArray(); // Identity roles ophalen.
            result.Add(new AdminUserDto( // Projectie naar API-contract.
                u.Id,
                u.Email ?? "",
                u.DisplayName ?? "",
                u.IsBlocked,
                roles
            ));
        }

        return Ok(result); // 200 + lijst.
    }

    [HttpPost("users/{id}/toggle-block")] // POST api/admin/users/{id}/toggle-block: block status wisselen.
    public async Task<ActionResult> ToggleBlock(string id)
    {
        var meId = _userManager.GetUserId(User); // Huidige admin id uit JWT.
        if (!string.IsNullOrWhiteSpace(meId) && meId == id) // Self-block voorkomen.
            return BadRequest("You cannot block yourself.");

        var user = await _userManager.FindByIdAsync(id); // User opzoeken.
        if (user == null)
            return NotFound("User not found.");

        user.IsBlocked = !user.IsBlocked; // Toggle block flag.

        var result = await _userManager.UpdateAsync(user); // Update via Identity pipeline.
        if (!result.Succeeded)
            return BadRequest(string.Join(" | ", result.Errors.Select(e => e.Description))); // Validatie/errors bundelen.

        return Ok(); // 200 zonder body.
    }

    [HttpPut("users/{id}/role")] // PUT api/admin/users/{id}/role: user krijgt exact 1 rol.
    public async Task<ActionResult> SetSingleRole(string id, [FromBody] SetRoleRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Role)) // Basis input-validatie.
            return BadRequest("Role is required.");

        var role = req.Role.Trim(); // Normaliseren.

        if (!AllowedRoles.Contains(role)) // Alleen whitelist-rollen toelaten.
            return BadRequest("Invalid role.");

        if (!await _roleManager.RoleExistsAsync(role)) // Rol moet effectief bestaan in Identity tables.
            return BadRequest("Role does not exist on server.");

        var meId = _userManager.GetUserId(User); // Huidige admin id.
        if (!string.IsNullOrWhiteSpace(meId) && meId == id && role != "Admin") // Admin kan zichzelf niet degraderen.
            return BadRequest("You cannot remove your own Admin role.");

        var user = await _userManager.FindByIdAsync(id); // Doeluser ophalen.
        if (user == null)
            return NotFound("User not found.");

        var currentRoles = await _userManager.GetRolesAsync(user); // Huidige rollen ophalen.

        var toRemove = currentRoles.Where(r => AllowedRoles.Contains(r)).ToArray(); // AllowedRoles eerst verwijderen.
        if (toRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove); // Rollen verwijderen.
            if (!removeResult.Succeeded)
                return BadRequest(string.Join(" | ", removeResult.Errors.Select(e => e.Description)));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role); // Nieuwe rol toevoegen.
        if (!addResult.Succeeded)
            return BadRequest(string.Join(" | ", addResult.Errors.Select(e => e.Description)));

        return Ok(); // 200.
    }
}
