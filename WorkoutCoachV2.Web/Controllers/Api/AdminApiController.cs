using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/admin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminOnly")]
public class AdminApiController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    private static readonly string[] AllowedRoles = ["Admin", "Moderator", "User"];

    public AdminApiController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public record AdminUserDto(string Id, string Email, string DisplayName, bool IsBlocked, string[] Roles);
    public record SetRoleRequest(string Role);

    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email)
            .ToListAsync();

        var result = new List<AdminUserDto>();

        foreach (var u in users)
        {
            var roles = (await _userManager.GetRolesAsync(u)).ToArray();
            result.Add(new AdminUserDto(
                u.Id,
                u.Email ?? "",
                u.DisplayName ?? "",
                u.IsBlocked,
                roles
            ));
        }

        return Ok(result);
    }

    [HttpPost("users/{id}/toggle-block")]
    public async Task<ActionResult> ToggleBlock(string id)
    {
        var meId = _userManager.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(meId) && meId == id)
            return BadRequest("You cannot block yourself.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("User not found.");

        user.IsBlocked = !user.IsBlocked;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(string.Join(" | ", result.Errors.Select(e => e.Description)));

        return Ok();
    }

    [HttpPut("users/{id}/role")]
    public async Task<ActionResult> SetSingleRole(string id, [FromBody] SetRoleRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Role))
            return BadRequest("Role is required.");

        var role = req.Role.Trim();

        if (!AllowedRoles.Contains(role))
            return BadRequest("Invalid role.");

        if (!await _roleManager.RoleExistsAsync(role))
            return BadRequest("Role does not exist on server.");

        var meId = _userManager.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(meId) && meId == id && role != "Admin")
            return BadRequest("You cannot remove your own Admin role.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("User not found.");

        var currentRoles = await _userManager.GetRolesAsync(user);

        var toRemove = currentRoles.Where(r => AllowedRoles.Contains(r)).ToArray();
        if (toRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
                return BadRequest(string.Join(" | ", removeResult.Errors.Select(e => e.Description)));
        }

        var addResult = await _userManager.AddToRoleAsync(user, role);
        if (!addResult.Succeeded)
            return BadRequest(string.Join(" | ", addResult.Errors.Select(e => e.Description)));

        return Ok();
    }
}
