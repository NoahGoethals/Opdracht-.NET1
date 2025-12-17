namespace WorkoutCoachV3.Maui.Services;

public interface ITokenStore
{
    Task SetAsync(string token, DateTime expiresUtc);
    Task<string?> GetTokenAsync();
    Task<DateTime?> GetExpiresUtcAsync();
    Task ClearAsync();
}
