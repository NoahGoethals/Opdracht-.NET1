// TokenStore: bewaart JWT + expiry in SecureStorage en als fallback in Preferences.
using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;

namespace WorkoutCoachV3.Maui.Services;

public class TokenStore : ITokenStore
{
    // Keys voor token en expiry opslag.
    private const string TokenKey = "auth_token";
    private const string ExpiresKey = "auth_expires_utc";
    // Kleine marge om net-verlopen tokens te vermijden.
    private static readonly TimeSpan ExpirySkew = TimeSpan.FromSeconds(30);

    // Slaat token + expiry op (UTC) in secure storage en preferences.
    public async Task SetAsync(string token, DateTime expiresUtc)
    {
        var utc = expiresUtc.Kind switch
        {
            DateTimeKind.Utc => expiresUtc,
            DateTimeKind.Local => expiresUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(expiresUtc, DateTimeKind.Utc)
        };

        var expiresString = utc.ToString("O", CultureInfo.InvariantCulture);

        _ = await TrySecureSetAsync(TokenKey, token);
        _ = await TrySecureSetAsync(ExpiresKey, expiresString);

        Preferences.Set(TokenKey, token);
        Preferences.Set(ExpiresKey, expiresString);
    }

    // Geeft token terug of null (en wist als hij (bijna) verlopen is).
    public async Task<string?> GetTokenAsync()
    {
        var secure = await TrySecureGetAsync(TokenKey);
        var token = !string.IsNullOrWhiteSpace(secure)
            ? secure
            : Preferences.Get(TokenKey, null);

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var expires = await GetExpiresUtcAsync();
        if (expires.HasValue && DateTime.UtcNow >= (expires.Value - ExpirySkew)) 
        {
            await ClearAsync();
            return null;
        }

        return token;
    }

    // Haalt expiry op; probeert ook uit JWT "exp" te lezen als nodig.
    public async Task<DateTime?> GetExpiresUtcAsync()
    {
        var secure = await TrySecureGetAsync(ExpiresKey);
        var str = !string.IsNullOrWhiteSpace(secure)
            ? secure
            : Preferences.Get(ExpiresKey, null);

        if (!string.IsNullOrWhiteSpace(str))
        {
            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            {
                if (dt.Kind == DateTimeKind.Unspecified)
                    dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                return dt.ToUniversalTime();
            }
        }

        var token = await TrySecureGetAsync(TokenKey);
        token = !string.IsNullOrWhiteSpace(token) ? token : Preferences.Get(TokenKey, null);

        var fromJwt = TryGetExpiryFromJwt(token);
        if (fromJwt.HasValue)
        {
            var expiresString = fromJwt.Value.ToString("O", CultureInfo.InvariantCulture);
            _ = await TrySecureSetAsync(ExpiresKey, expiresString);
            Preferences.Set(ExpiresKey, expiresString);
        }

        return fromJwt;
    }

    // True als token bestaat en expiry nog niet (bijna) bereikt is.
    public async Task<bool> HasValidTokenAsync()
    {
        var token = await GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            return false;

        var expires = await GetExpiresUtcAsync();
        if (!expires.HasValue)
            return false;

        return DateTime.UtcNow < (expires.Value - ExpirySkew);
    }

    // Verwijdert token + expiry uit SecureStorage en Preferences.
    public async Task ClearAsync()
    {
        await TrySecureRemoveAsync(TokenKey);
        await TrySecureRemoveAsync(ExpiresKey);

        Preferences.Remove(TokenKey);
        Preferences.Remove(ExpiresKey);
    }

    // Probeert expiry te lezen uit JWT payload claim "exp".
    private static DateTime? TryGetExpiryFromJwt(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return null;

        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;

        try
        {
            var jsonBytes = Base64UrlDecode(parts[1]);
            using var doc = JsonDocument.Parse(jsonBytes);

            if (doc.RootElement.TryGetProperty("exp", out var expEl) && expEl.TryGetInt64(out var exp))
                return DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

            return null;
        }
        catch
        {
            return null;
        }
    }

    // Base64Url decode (JWT payload gebruikt -/_ i.p.v. +/).
    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        var pad = 4 - (s.Length % 4);
        if (pad is > 0 and < 4)
            s = s + new string('=', pad);

        return Convert.FromBase64String(s);
    }

    // SecureStorage set met swallow van platform exceptions.
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

    // SecureStorage get met swallow van platform exceptions.
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

    // SecureStorage remove (best effort).
    private static Task TrySecureRemoveAsync(string key)
    {
        try { SecureStorage.Remove(key); } catch { }
        return Task.CompletedTask;
    }
}
