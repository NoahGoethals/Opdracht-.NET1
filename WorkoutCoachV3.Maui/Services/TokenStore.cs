namespace WorkoutCoachV3.Maui.Services;

public class TokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";
    private const string ExpiresKey = "auth_expires_utc";

    public Task SetAsync(string token, DateTime expiresUtc)
    {
        Preferences.Set(TokenKey, token);
        Preferences.Set(ExpiresKey, expiresUtc.ToString("O"));
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync()
        => Task.FromResult(Preferences.Get(TokenKey, null as string));

    public Task<DateTime?> GetExpiresUtcAsync()
    {
        var s = Preferences.Get(ExpiresKey, null as string);
        if (string.IsNullOrWhiteSpace(s)) return Task.FromResult<DateTime?>(null);
        if (DateTime.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            return Task.FromResult<DateTime?>(dt);
        return Task.FromResult<DateTime?>(null);
    }

    public Task ClearAsync()
    {
        Preferences.Remove(TokenKey);
        Preferences.Remove(ExpiresKey);
        return Task.CompletedTask;
    }
}
