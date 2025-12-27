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

[ApiController] // API controller voor auth endpoints.
[Route("api/[controller]")] // api/auth/...
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager; // Users vinden/aanmaken + rollen ophalen.
    private readonly SignInManager<ApplicationUser> _signInManager; // Password sign-in checks volgens Identity regels.
    private readonly IConfiguration _config; // Lezen van Jwt:* settings.

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config)
    {
        _userManager = userManager; // DI.
        _signInManager = signInManager; // DI.
        _config = config; // DI.
    }

    [HttpPost("login")] // POST api/auth/login: JWT token verkrijgen.
    [AllowAnonymous] // Publiek endpoint.
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var user = await _userManager.FindByEmailAsync(req.Email); // User zoeken op email.
        if (user == null) return Unauthorized("Ongeldige login."); // Geen user => 401.

        if (user.IsBlocked) return StatusCode(403, "Gebruiker is geblokkeerd."); // Block => 403.

        var ok = await _signInManager.CheckPasswordSignInAsync(user, req.Password, lockoutOnFailure: false); // Password check.
        if (!ok.Succeeded) return Unauthorized("Ongeldige login."); // Fout wachtwoord => 401.

        var roles = (await _userManager.GetRolesAsync(user)).ToArray(); // Rollen voor claims.
        var token = CreateJwt(user, roles, out var expiresUtc); // JWT bouwen.

        return Ok(new AuthResponse(token, expiresUtc, user.Id, user.Email ?? "", user.DisplayName ?? "", roles)); // Token + user info.
    }

    [HttpPost("register")] // POST api/auth/register: user aanmaken + JWT teruggeven.
    [AllowAnonymous] // Publiek endpoint.
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var existing = await _userManager.FindByEmailAsync(req.Email); // Dubbel email check.
        if (existing != null) return BadRequest("Email bestaat al.");

        var user = new ApplicationUser // Custom Identity user (met DisplayName/IsBlocked).
        {
            UserName = req.Email,
            Email = req.Email,
            DisplayName = req.DisplayName,
            EmailConfirmed = true, // Email verificatie wordt hier overgeslagen (confirmed).
            IsBlocked = false
        };

        var result = await _userManager.CreateAsync(user, req.Password); // Identity create.
        if (!result.Succeeded) return BadRequest(result.Errors.Select(e => e.Description)); // Identity errors.

        await _userManager.AddToRoleAsync(user, "User"); // Standaardrol toekennen bij registratie.

        var roles = (await _userManager.GetRolesAsync(user)).ToArray(); // Rollen voor token.
        var token = CreateJwt(user, roles, out var expiresUtc); // JWT.

        return Ok(new AuthResponse(token, expiresUtc, user.Id, user.Email ?? "", user.DisplayName ?? "", roles)); // Response.
    }

    [HttpGet("me")] // GET api/auth/me: huidige user info via JWT.
    [Authorize] // Vereist ingelogde user.
    public async Task<ActionResult<CurrentUserDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User); // User uit principal.
        if (user == null) return Unauthorized();

        if (user.IsBlocked) return StatusCode(403, "Gebruiker is geblokkeerd."); // Block => 403.

        var roles = (await _userManager.GetRolesAsync(user)).ToArray(); // Rollen ophalen.

        return Ok(new CurrentUserDto( // DTO voor client.
            user.Id,
            user.Email ?? "",
            user.DisplayName ?? "",
            roles
        ));
    }

    private string CreateJwt(ApplicationUser user, string[] roles, out DateTime expiresUtc) // JWT builder helper.
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key ontbreekt"); // Signing key (vereist).
        var issuer = _config["Jwt:Issuer"] ?? "WorkoutCoachV2"; // Issuer (default fallback).
        var audience = _config["Jwt:Audience"] ?? "WorkoutCoachV2.Maui"; // Audience (default fallback).
        var expiresMinutes = int.TryParse(_config["Jwt:ExpiresMinutes"], out var m) ? m : 120; // Expiry in minuten.

        var claims = new List<Claim> // Basis claims voor identificatie.
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? "")
        };

        foreach (var r in roles) // Rol-claims toevoegen voor autorisatie/policies.
            claims.Add(new Claim(ClaimTypes.Role, r));

        expiresUtc = DateTime.UtcNow.AddMinutes(expiresMinutes); // Expiry timestamp.

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)); // Symmetric signing key.
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256); // HMAC SHA256 signing.

        var jwt = new JwtSecurityToken( // JWT token object.
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt); // Serialize token naar string.
    }
}
