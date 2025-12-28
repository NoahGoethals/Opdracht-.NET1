// Token opslag contract (JWT + expiry) voor auth en automatische refresh logic.
namespace WorkoutCoachV3.Maui.Services;

public interface ITokenStore
{
    // Slaat token + vervaldatum (UTC) op.
    Task SetAsync(string token, DateTime expiresUtc);

    // Haalt de huidige token op (null als geen token).
    Task<string?> GetTokenAsync();

    // Haalt expiry op (null als onbekend).
    Task<DateTime?> GetExpiresUtcAsync();

    // True wanneer er een token is die nog niet verlopen is.
    Task<bool> HasValidTokenAsync();

    // Verwijdert token + expiry (logout/unauthorized).
    Task ClearAsync();
}
