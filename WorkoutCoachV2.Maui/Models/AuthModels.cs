namespace WorkoutCoachV2.Maui.Models;

public record LoginRequest(string Email, string Password);

public record AuthResponse(
    string Token,
    DateTime ExpiresUtc,
    string UserId,
    string Email,
    string DisplayName,
    string[] Roles
);
