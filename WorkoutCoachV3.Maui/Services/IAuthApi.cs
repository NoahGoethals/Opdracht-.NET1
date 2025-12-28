// Contract voor auth calls: login, register en "me" (huidige user).
namespace WorkoutCoachV3.Maui.Services;

using WorkoutCoachV2.Model.ApiContracts;

public interface IAuthApi
{
    // Login met credentials; verwacht AuthResponse (tokens/user info).
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    // Register nieuw account; verwacht AuthResponse terug.
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    // Haalt de huidige user op op basis van opgeslagen token.
    Task<CurrentUserDto> MeAsync(CancellationToken ct = default);
}
