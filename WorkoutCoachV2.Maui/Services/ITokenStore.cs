namespace WorkoutCoachV2.Maui.Services;

public interface ITokenStore
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token, DateTime expiresUtc);
    Task ClearAsync();
}
