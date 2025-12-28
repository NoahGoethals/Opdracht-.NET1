// Contract voor volledige sync-run (push/pull alles wat offline staat).
namespace WorkoutCoachV3.Maui.Services;

public interface ISyncService
{
    // Start de volledige synchronisatie flow (meestal meerdere tabellen/requests).
    Task SyncAllAsync(CancellationToken ct = default);
}
