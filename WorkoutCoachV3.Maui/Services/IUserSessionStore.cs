// User sessie opslag contract (id/email/displayName/roles) voor UI en autorisatie.
namespace WorkoutCoachV3.Maui.Services;

public interface IUserSessionStore
{
    // Slaat user info op na login/me call.
    Task SetAsync(string userId, string email, string displayName, string[] roles);
    // Basisvelden ophalen voor UI.
    Task<string?> GetUserIdAsync();
    Task<string?> GetEmailAsync();
    Task<string?> GetDisplayNameAsync();
    // Roles ophalen voor UI (admin knoppen) en checks.
    Task<string[]> GetRolesAsync();
    // Role checks voor conditionele UI (bv. CanAccessAdmin).
    Task<bool> IsInRoleAsync(string role);
    Task<bool> IsInAnyRoleAsync(params string[] roles);
    // Leegt alles (logout/unauthorized).
    Task ClearAsync();
}
