using System.Text.Json;

namespace WorkoutCoachV3.Maui.Services;

public class UserSessionStore : IUserSessionStore
{
    private const string UserIdKey = "user_id";
    private const string EmailKey = "user_email";
    private const string DisplayNameKey = "user_display_name";
    private const string RolesKey = "user_roles_json";

    public Task SetAsync(string userId, string email, string displayName, string[] roles)
    {
        Preferences.Set(UserIdKey, userId ?? "");
        Preferences.Set(EmailKey, email ?? "");
        Preferences.Set(DisplayNameKey, displayName ?? "");
        Preferences.Set(RolesKey, JsonSerializer.Serialize(roles ?? Array.Empty<string>()));
        return Task.CompletedTask;
    }

    public Task<string?> GetUserIdAsync()
        => Task.FromResult(Preferences.Get(UserIdKey, (string?)null));

    public Task<string?> GetEmailAsync()
        => Task.FromResult(Preferences.Get(EmailKey, (string?)null));

    public Task<string?> GetDisplayNameAsync()
        => Task.FromResult(Preferences.Get(DisplayNameKey, (string?)null));

    public Task<string[]> GetRolesAsync()
    {
        var json = Preferences.Get(RolesKey, "");
        if (string.IsNullOrWhiteSpace(json))
            return Task.FromResult(Array.Empty<string>());

        try
        {
            var roles = JsonSerializer.Deserialize<string[]>(json);
            return Task.FromResult(roles ?? Array.Empty<string>());
        }
        catch
        {
            return Task.FromResult(Array.Empty<string>());
        }
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        var roles = await GetRolesAsync();
        return roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> IsInAnyRoleAsync(params string[] roles)
    {
        var mine = await GetRolesAsync();
        return mine.Any(r => roles.Any(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase)));
    }

    public Task ClearAsync()
    {
        Preferences.Remove(UserIdKey);
        Preferences.Remove(EmailKey);
        Preferences.Remove(DisplayNameKey);
        Preferences.Remove(RolesKey);
        return Task.CompletedTask;
    }
}
