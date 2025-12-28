// Contract om te testen of de API bereikbaar is (zonder echte data-opvraging).
namespace WorkoutCoachV3.Maui.Services;

public interface IApiHealthService
{
    // Geeft true terug als de API reageert (of minstens bereikbaar is).
    Task<bool> IsApiReachableAsync(CancellationToken ct = default);
}
