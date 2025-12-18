namespace WorkoutCoachV3.Maui.Services;

public interface ISyncService
{
    Task SyncAllAsync(CancellationToken ct = default);
}
