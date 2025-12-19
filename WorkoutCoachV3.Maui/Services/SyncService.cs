using Microsoft.Maui.Networking;
using System.Diagnostics;

namespace WorkoutCoachV3.Maui.Services;

public class SyncService : ISyncService
{
    private readonly LocalDatabaseService _local;
    private readonly IExercisesApi _exercisesApi;
    private readonly IWorkoutsApi _workoutsApi;
    private readonly IWorkoutExercisesApi _workoutExercisesApi;
    private readonly ISessionsApi _sessionsApi;

    public SyncService(
        LocalDatabaseService local,
        IExercisesApi exercisesApi,
        IWorkoutsApi workoutsApi,
        IWorkoutExercisesApi workoutExercisesApi,
        ISessionsApi sessionsApi)
    {
        _local = local;
        _exercisesApi = exercisesApi;
        _workoutsApi = workoutsApi;
        _workoutExercisesApi = workoutExercisesApi;
        _sessionsApi = sessionsApi;
    }

    public async Task SyncAllAsync(CancellationToken ct = default)
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            return;

        // Push first (offline changes)
        await SyncExercisesPushAsync(ct);
        await SyncWorkoutsPushAsync(ct);
        await SyncWorkoutExercisesPushAsync(ct);
        await SyncSessionsPushAsync(ct);

        // Pull afterwards (server truth)
        await SyncExercisesPullAsync(ct);
        await SyncWorkoutsPullAsync(ct);
        await SyncWorkoutExercisesPullAsync(ct);
        await SyncSessionsPullAsync(ct);
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
            catch (Exception ex)
            {
                Debug.WriteLine("[SYNC][PUSH][EXERCISES] " + ex);
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
            catch (Exception ex)
            {
                Debug.WriteLine("[SYNC][PUSH][WORKOUTS] " + ex);
            }
        }
    }

    private async Task SyncWorkoutExercisesPushAsync(CancellationToken ct)
    {
        var dirtyLinks = await _local.GetDirtyWorkoutExercisesAsync();
        if (dirtyLinks.Count == 0) return;

        var workoutIds = dirtyLinks.Select(x => x.WorkoutLocalId).Distinct().ToList();

        foreach (var workoutLocalId in workoutIds)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var workout = await _local.GetWorkoutByLocalIdAsync(workoutLocalId);
                if (workout?.RemoteId is null) continue;

                var allLinks = await _local.GetWorkoutExercisesAllStatesAsync(workoutLocalId);
                var active = allLinks.Where(x => !x.IsDeleted).ToList();

                var allExercises = await _local.GetExercisesAsync();
                var exMap = allExercises
                    .Where(e => e.RemoteId.HasValue && !e.IsDeleted)
                    .ToDictionary(e => e.LocalId, e => e.RemoteId!.Value);

                var payload = new List<UpsertWorkoutExerciseLinkDto>();

                foreach (var link in active)
                {
                    if (!exMap.TryGetValue(link.ExerciseLocalId, out var remoteExerciseId))
                        continue;

                    payload.Add(new UpsertWorkoutExerciseLinkDto
                    {
                        ExerciseId = remoteExerciseId,
                        Reps = Math.Max(0, link.Repetitions),
                        WeightKg = Math.Max(0.0, link.WeightKg)
                    });
                }

                await _workoutExercisesApi.ReplaceAllAsync(workout.RemoteId.Value, payload, ct);
                await _local.MarkWorkoutExercisesSyncedAsync(workoutLocalId);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SYNC][PUSH][WORKOUT-EXERCISES] " + ex);
            }
        }
    }

    private async Task SyncSessionsPushAsync(CancellationToken ct)
    {
        var dirty = await _local.GetDirtySessionsAsync();
        if (dirty.Count == 0) return;

        // Build local exercise -> remote exercise id map once
        var localExercises = await _local.GetExercisesAsync();
        var exLocalToRemote = localExercises
            .Where(e => e.RemoteId.HasValue && !e.IsDeleted)
            .ToDictionary(e => e.LocalId, e => e.RemoteId!.Value);

        foreach (var s in dirty)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                if (s.IsDeleted)
                {
                    if (s.RemoteId.HasValue)
                        await _sessionsApi.DeleteAsync(s.RemoteId.Value, ct);

                    await _local.HardDeleteSessionAsync(s.LocalId);
                    continue;
                }

                // Collect sets for this session and map exercise local ids -> remote ids
                var setEntities = await _local.GetSessionSetsEntitiesAsync(s.LocalId, includeDeleted: false);

                // If session contains at least one exercise that isn't synced yet, skip pushing.
                // It will be pushed after Exercises sync gives those exercises a RemoteId.
                if (setEntities.Any(x => !exLocalToRemote.ContainsKey(x.ExerciseLocalId)))
                    continue;

                var sets = setEntities
                    .Select(x => new SessionSetDto(
                        ExerciseId: exLocalToRemote[x.ExerciseLocalId],
                        SetNumber: Math.Max(1, x.SetNumber),
                        Reps: Math.Max(0, x.Reps),
                        Weight: Math.Max(0.0, x.Weight)
                    ))
                    .OrderBy(x => x.ExerciseId)
                    .ThenBy(x => x.SetNumber)
                    .ToList();

                if (!s.RemoteId.HasValue)
                {
                    var created = await _sessionsApi.CreateAsync(
                        new CreateSessionDto(
                            Title: s.Title,
                            Date: s.Date,
                            Description: s.Description,
                            Sets: sets
                        ),
                        ct);

                    await _local.MarkSessionSyncedAsync(s.LocalId, created.Id);
                }
                else
                {
                    await _sessionsApi.UpdateAsync(
                        s.RemoteId.Value,
                        new UpdateSessionDto(
                            Title: s.Title,
                            Date: s.Date,
                            Description: s.Description,
                            Sets: sets
                        ),
                        ct);

                    await _local.MarkSessionSyncedAsync(s.LocalId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[SYNC][PUSH][SESSIONS] " + ex);
            }
        }
    }

    private async Task SyncExercisesPullAsync(CancellationToken ct)
    {
        try
        {
            var remote = await _exercisesApi.GetAllAsync(search: null, category: null, sort: "name", ct: ct);

            await _local.MergeRemoteExercisesAsync(
                remote.Select(x => (x.Id, x.Name, x.Category, x.Notes)).ToList());
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[SYNC][PULL][EXERCISES] " + ex);
        }
    }

    private async Task SyncWorkoutsPullAsync(CancellationToken ct)
    {
        try
        {
            var remote = await _workoutsApi.GetAllAsync(search: null, sort: "title", ct: ct);

            await _local.MergeRemoteWorkoutsAsync(
                remote.Select(x => (x.Id, x.Title)).ToList());
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[SYNC][PULL][WORKOUTS] " + ex);
        }
    }

    private async Task SyncWorkoutExercisesPullAsync(CancellationToken ct)
    {
        try
        {
            var localWorkouts = await _local.GetWorkoutsAsync();
            var withRemote = localWorkouts.Where(w => w.RemoteId.HasValue && !w.IsDeleted).ToList();
            if (withRemote.Count == 0) return;

            var localExercises = await _local.GetExercisesAsync();
            var exerciseRemoteToLocal = localExercises
                .Where(e => e.RemoteId.HasValue && !e.IsDeleted)
                .ToDictionary(e => e.RemoteId!.Value, e => e.LocalId);

            foreach (var w in withRemote)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var remoteLinks = await _workoutExercisesApi.GetAllAsync(w.RemoteId!.Value, ct);

                    var mapped = remoteLinks
                        .Where(r => exerciseRemoteToLocal.ContainsKey(r.ExerciseId))
                        .Select(r => (
                            ExerciseLocalId: exerciseRemoteToLocal[r.ExerciseId],
                            Repetitions: Math.Max(0, r.Reps),
                            WeightKg: Math.Max(0.0, r.WeightKg ?? 0.0)
                        ))
                        .ToList();

                    await _local.ReplaceWorkoutExercisesFromRemoteAsync(w.LocalId, mapped);
                }
                catch (Exception exWorkout)
                {
                    Debug.WriteLine($"[SYNC][PULL][WORKOUT-EXERCISES][workoutRemoteId={w.RemoteId}] " + exWorkout);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[SYNC][PULL][WORKOUT-EXERCISES] " + ex);
        }
    }

    private async Task SyncSessionsPullAsync(CancellationToken ct)
    {
        try
        {
            var remote = await _sessionsApi.GetAllAsync(
                search: null,
                from: null,
                to: null,
                sort: "date_desc",
                includeSets: true,
                ct: ct);

            await _local.MergeRemoteSessionsAsync(remote);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[SYNC][PULL][SESSIONS] " + ex);
        }
    }
}
