namespace WorkoutCoachV2.Maui.Services;

public record AuthResponse(string Token, DateTime ExpiresUtc, string UserId, string Email, string DisplayName, string[] Roles);

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(string email, string password);
}
