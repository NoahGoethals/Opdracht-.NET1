namespace WorkoutCoachV2.Maui.Services;

public class TokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";
    private const string ExpKey = "auth_expires_utc";

    public Task<string?> GetTokenAsync()
    {
        var token = Preferences.Get(TokenKey, null);
        var expStr = Preferences.Get(ExpKey, null);

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(expStr))
            return Task.FromResult<string?>(null);

        if (DateTime.TryParse(expStr, out var expUtc) && expUtc > DateTime.UtcNow)
            return Task.FromResult<string?>(token);

        Preferences.Remove(TokenKey);
        Preferences.Remove(ExpKey);
        return Task.FromResult<string?>(null);
    }

    public Task SetTokenAsync(string token, DateTime expiresUtc)
    {
        Preferences.Set(TokenKey, token);
        Preferences.Set(ExpKey, expiresUtc.ToString("O"));
        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        Preferences.Remove(TokenKey);
        Preferences.Remove(ExpKey);
        return Task.CompletedTask;
    }
}
