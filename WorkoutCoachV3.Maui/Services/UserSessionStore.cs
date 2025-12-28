using System.Text.Json;

namespace WorkoutCoachV3.Maui.Services;

// Bewaart ingelogde user info lokaal (Preferences) zodat UI/roles beschikbaar blijven.
public class UserSessionStore : IUserSessionStore
{
    // Keys in Preferences voor user gegevens + roles als JSON.
    private const string UserIdKey = "user_id";
    private const string EmailKey = "user_email";
    private const string DisplayNameKey = "user_display_name";
    private const string RolesKey = "user_roles_json";

    // Slaat user gegevens op na login/me call.
    public Task SetAsync(string userId, string email, string displayName, string[] roles)
    {
        Preferences.Set(UserIdKey, userId ?? "");
        Preferences.Set(EmailKey, email ?? "");
        Preferences.Set(DisplayNameKey, displayName ?? "");
        Preferences.Set(RolesKey, JsonSerializer.Serialize(roles ?? Array.Empty<string>()));
        return Task.CompletedTask;
    }

    // Leest user id uit Preferences (kan null zijn).
    public Task<string?> GetUserIdAsync()
        => Task.FromResult(Preferences.Get(UserIdKey, (string?)null));

    // Leest email uit Preferences.
    public Task<string?> GetEmailAsync()
        => Task.FromResult(Preferences.Get(EmailKey, (string?)null));

    // Leest display name uit Preferences.
    public Task<string?> GetDisplayNameAsync()
        => Task.FromResult(Preferences.Get(DisplayNameKey, (string?)null));

    // Leest roles als JSON array; valt terug naar empty bij errors.
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

    // Checkt of user een specifieke role heeft (case-insensitive).
    public async Task<bool> IsInRoleAsync(string role)
    {
        var roles = await GetRolesAsync();
        return roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
    }

    // Checkt of user in één van de opgegeven roles zit.
    public async Task<bool> IsInAnyRoleAsync(params string[] roles)
    {
        var mine = await GetRolesAsync();
        return mine.Any(r => roles.Any(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase)));
    }

    // Logout/clear: verwijdert alle opgeslagen session keys.
    public Task ClearAsync()
    {
        Preferences.Remove(UserIdKey);
        Preferences.Remove(EmailKey);
        Preferences.Remove(DisplayNameKey);
        Preferences.Remove(RolesKey);
        return Task.CompletedTask;
    }
}
