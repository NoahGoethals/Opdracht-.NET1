using Microsoft.Maui.Networking;
using WorkoutCoachV3.Maui.Data;
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Services;

public class SyncService : ISyncService
{
    private readonly LocalDatabaseService _local;
    private readonly IExercisesApi _exercisesApi;

    public SyncService(LocalDatabaseService local, IExercisesApi exercisesApi)
    {
        _local = local;
        _exercisesApi = exercisesApi;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return;

        await SyncExercisesAsync(ct);

        
    }

    private async Task SyncExercisesAsync(CancellationToken ct)
    {
        var dirty = await _local.GetDirtyExercisesAsync();

        foreach (var e in dirty)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (e.IsDeleted)
                {
                    if (e.RemoteId.HasValue)
                    {
                        await _exercisesApi.DeleteAsync(e.RemoteId.Value, ct);
                    }

                    await _local.HardDeleteExerciseAsync(e.LocalId);
                    continue;
                }

                if (!e.RemoteId.HasValue)
                {
                    var created = await _exercisesApi.CreateAsync(
                        new CreateExerciseDto(e.Name, e.Category, e.Notes),
                        ct);

                    await _local.MarkExerciseSyncedAsync(e.LocalId, created.Id);
                }
                else
                {
                    await _exercisesApi.UpdateAsync(
                        e.RemoteId.Value,
                        new UpdateExerciseDto(e.Name, e.Category, e.Notes),
                        ct);

                    await _local.MarkExerciseSyncedAsync(e.LocalId);
                }
            }
            catch
            {
            }
        }

        var remote = await _exercisesApi.GetAllAsync(search: null, category: null, sort: "name", ct: ct);

        await _local.MergeRemoteExercisesAsync(
            remote.Select(x => (x.Id, x.Name, x.Category, x.Notes)).ToList());
    }
}
