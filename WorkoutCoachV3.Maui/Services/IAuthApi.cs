namespace WorkoutCoachV3.Maui.Services;

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(string email, string password);
}

public record AuthResponse(string Token, DateTime ExpiresUtc, string UserId, string Email, string DisplayName, string[] Roles);
