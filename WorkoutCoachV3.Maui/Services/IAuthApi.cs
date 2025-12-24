namespace WorkoutCoachV3.Maui.Services;

using WorkoutCoachV2.Model.ApiContracts;

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
}
