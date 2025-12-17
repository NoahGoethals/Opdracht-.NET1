using Microsoft.Maui.Storage;

namespace WorkoutCoachV2.Maui.Services;

public interface ITokenStore
{
    Task SaveAsync(string token, DateTime expiresUtc, string email);
    Task<(string? token, DateTime? expiresUtc, string? email)> LoadAsync();
    Task ClearAsync();
}

public class TokenStore : ITokenStore
{
    private const string KeyToken = "auth_token";
    private const string KeyExpires = "auth_expiresUtc";
    private const string KeyEmail = "auth_email";

    public async Task SaveAsync(string token, DateTime expiresUtc, string email)
    {
        await SecureStorage.SetAsync(KeyToken, token);
        await SecureStorage.SetAsync(KeyExpires, expiresUtc.ToString("O"));
        await SecureStorage.SetAsync(KeyEmail, email);
    }

    public async Task<(string? token, DateTime? expiresUtc, string? email)> LoadAsync()
    {
        var token = await SecureStorage.GetAsync(KeyToken);
        var expStr = await SecureStorage.GetAsync(KeyExpires);
        var email = await SecureStorage.GetAsync(KeyEmail);

        DateTime? exp = null;
        if (DateTime.TryParse(expStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            exp = parsed;

        return (token, exp, email);
    }

    public Task ClearAsync()
    {
        SecureStorage.Remove(KeyToken);
        SecureStorage.Remove(KeyExpires);
        SecureStorage.Remove(KeyEmail);
        return Task.CompletedTask;
    }
}
