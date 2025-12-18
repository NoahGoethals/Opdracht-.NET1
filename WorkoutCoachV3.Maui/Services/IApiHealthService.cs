namespace WorkoutCoachV3.Maui.Services;

public interface IApiHealthService
{
    Task<bool> IsApiReachableAsync(CancellationToken ct = default);
}
