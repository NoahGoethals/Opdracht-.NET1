using Microsoft.Maui.Networking;
using WorkoutCoachV3.Maui.Apis;
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Services;

public class SyncService : ISyncService
{
    private readonly LocalDatabaseService _local;
    private readonly IExercisesApi _exercisesApi;
    private readonly IWorkoutsApi _workoutsApi;

    public SyncService(LocalDatabaseService local, IExercisesApi exercisesApi, IWorkoutsApi workoutsApi)
    {
        _local = local;
        _exercisesApi = exercisesApi;
        _workoutsApi = workoutsApi;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return;

        await SyncExercisesPushAsync(ct);
        await SyncWorkoutsPushAsync(ct);

        await SyncExercisesPullAsync(ct);
        await SyncWorkoutsPullAsync(ct);
    }

    private async Task SyncExercisesPushAsync(CancellationToken ct)
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
                        await _exercisesApi.DeleteAsync(e.RemoteId.Value, ct);

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
    }

    private async Task SyncWorkoutsPushAsync(CancellationToken ct)
    {
        var dirty = await _local.GetDirtyWorkoutsAsync();

        foreach (var w in dirty)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (w.IsDeleted)
                {
                    if (w.RemoteId.HasValue)
                        await _workoutsApi.DeleteAsync(w.RemoteId.Value, ct);

                    await _local.HardDeleteWorkoutAsync(w.LocalId);
                    continue;
                }

                if (!w.RemoteId.HasValue)
                {
                    var created = await _workoutsApi.CreateAsync(
                        new CreateWorkoutDto(w.Title, ScheduledOn: null),
                        ct);

                    await _local.MarkWorkoutSyncedAsync(w.LocalId, created.Id);
                }
                else
                {
                    await _workoutsApi.UpdateAsync(
                        w.RemoteId.Value,
                        new UpdateWorkoutDto(w.Title, ScheduledOn: null),
                        ct);

                    await _local.MarkWorkoutSyncedAsync(w.LocalId);
                }
            }
            catch
            {
            }
        }
    }

    private async Task SyncExercisesPullAsync(CancellationToken ct)
    {
        var remote = await _exercisesApi.GetAllAsync(search: null, category: null, sort: "name", ct: ct);

        await _local.MergeRemoteExercisesAsync(
            remote.Select(x => (x.Id, x.Name, x.Category, x.Notes)).ToList());
    }

    private async Task SyncWorkoutsPullAsync(CancellationToken ct)
    {
        var remote = await _workoutsApi.GetAllAsync(search: null, sort: "title", ct: ct);

        await _local.MergeRemoteWorkoutsAsync(
            remote.Select(x => (x.Id, x.Title)).ToList());
    }
}
