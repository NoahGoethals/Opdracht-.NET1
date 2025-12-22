namespace WorkoutCoachV3.Maui.Services;

public interface IUserSessionStore
{
    Task SetAsync(string userId, string email, string displayName, string[] roles);
    Task<string?> GetUserIdAsync();
    Task<string?> GetEmailAsync();
    Task<string?> GetDisplayNameAsync();
    Task<string[]> GetRolesAsync();
    Task<bool> IsInRoleAsync(string role);
    Task<bool> IsInAnyRoleAsync(params string[] roles);
    Task ClearAsync();
}
