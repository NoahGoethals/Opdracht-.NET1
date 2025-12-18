using System.Globalization;

namespace WorkoutCoachV3.Maui.Services;

public class TokenStore : ITokenStore
{
    private const string TokenKey = "auth_token";
    private const string ExpiresKey = "auth_expires_utc";

    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

    public async Task SetAsync(string token, DateTime expiresUtc)
    {
        var expiresString = expiresUtc.ToString("O");

        if (!await TrySecureSetAsync(TokenKey, token))
            Preferences.Set(TokenKey, token);

        if (!await TrySecureSetAsync(ExpiresKey, expiresString))
            Preferences.Set(ExpiresKey, expiresString);
    }

    public async Task<string?> GetTokenAsync()
    {
        if (!await HasValidTokenAsync())
            return null;

        var token = await TrySecureGetAsync(TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
            return token;

        token = Preferences.Get(TokenKey, null as string);
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public async Task<DateTime?> GetExpiresUtcAsync()
    {
        var s = await TrySecureGetAsync(ExpiresKey);
        if (string.IsNullOrWhiteSpace(s))
            s = Preferences.Get(ExpiresKey, null as string);

        if (string.IsNullOrWhiteSpace(s)) return null;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt;

        return null;
    }

    public async Task<bool> HasValidTokenAsync()
    {
        var token = await TrySecureGetAsync(TokenKey);
        if (string.IsNullOrWhiteSpace(token))
            token = Preferences.Get(TokenKey, null as string);

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var expires = await GetExpiresUtcAsync();
        if (expires is null)
            return false;

        var now = DateTime.UtcNow;
        var valid = now < (expires.Value - ExpirySkew);

        if (!valid)
            await ClearAsync();

        return valid;
    }

    public async Task ClearAsync()
    {
        await TrySecureRemoveAsync(TokenKey);
        await TrySecureRemoveAsync(ExpiresKey);

        Preferences.Remove(TokenKey);
        Preferences.Remove(ExpiresKey);
    }

    private static async Task<bool> TrySecureSetAsync(string key, string value)
    {
        try
        {
            await SecureStorage.SetAsync(key, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<string?> TrySecureGetAsync(string key)
    {
        try
        {
            return await SecureStorage.GetAsync(key);
        }
        catch
        {
            return null;
        }
    }

    private static async Task TrySecureRemoveAsync(string key)
    {
        try
        {
            SecureStorage.Remove(key);
            await Task.CompletedTask;
        }
        catch
        {
        }
    }
}
