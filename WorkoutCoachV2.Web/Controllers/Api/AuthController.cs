using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email);
        if (user == null) return Unauthorized("Ongeldige login.");

        if (user.IsBlocked) return StatusCode(403, "Gebruiker is geblokkeerd.");

        var ok = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false);
        if (!ok.Succeeded) return Unauthorized("Ongeldige login.");

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        var token = CreateJwt(user, roles, out var expiresUtc);

        return Ok(new AuthResponse(token, expiresUtc, user.Id, user.Email ?? "", user.DisplayName ?? "", roles));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var existing = await _userManager.FindByEmailAsync(req.Email);
        if (existing != null) return BadRequest("Email bestaat al.");

        var user = new ApplicationUser
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName,
            EmailConfirmed = true,
            IsBlocked = false
        };

        var result = await _userManager.CreateAsync(user, req.Password);
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, "User");

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();
        var token = CreateJwt(user, roles, out var expiresUtc);

        return Ok(new AuthResponse(token, expiresUtc, user.Id, user.Email ?? "", user.DisplayName ?? "", roles));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (user.IsBlocked) return StatusCode(403, "Gebruiker is geblokkeerd.");

        var roles = (await _userManager.GetRolesAsync(user)).ToArray();

        return Ok(new CurrentUserDto(
            user.Id,
            user.Email ?? "",
            user.DisplayName ?? "",
            roles
        ));
    }

    private string CreateJwt(ApplicationUser user, string[] roles, out DateTime expiresUtc)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ontbreekt");
        var issuer = _config["Jwt:Issuer"] ?? "WorkoutCoachV2";
        var audience = _config["Jwt:Audience"] ?? "WorkoutCoachV2.Maui";
        var expiresMinutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 120;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
        };

        foreach (var r in roles)
            claims.Add(new Claim(ClaimTypes.Role, r));

        expiresUtc = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
