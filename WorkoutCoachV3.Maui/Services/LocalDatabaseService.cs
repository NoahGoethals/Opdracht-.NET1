using Microsoft.EntityFrameworkCore;
using WorkoutCoachV3.Maui.Data;
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Services;

public class LocalDatabaseService
{
    private readonly IDbContextFactory<LocalAppDbContext> _dbFactory;

    public LocalDatabaseService(IDbContextFactory<LocalAppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task EnsureCreatedAndSeedAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        if (!await db.Exercises.AnyAsync())
        {
            db.Exercises.AddRange(
                new LocalExercise
                {
                    Name = "Bench Press",
                    Category = "Chest",
                    Notes = "Barbell",
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                },
                new LocalExercise
                {
                    Name = "Back Squat",
                    Category = "Legs",
                    Notes = "Depth focus",
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                },
                new LocalExercise
                {
                    Name = "Barbell Row",
                    Category = "Back",
                    Notes = "Strict form",
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                }
            );

            db.Workouts.Add(new LocalWorkout
            {
                Title = "Full Body A",
                Notes = "Seed workout",
                SyncState = SyncState.Synced,
                LastModifiedUtc = DateTime.UtcNow,
                LastSyncedUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }
    }

 

    public async Task<List<LocalExercise>> GetExercisesAsync(string? search = null, string? category = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<LocalExercise> q = db.Exercises.AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(x => x.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            category = category.Trim();
            if (!string.Equals(category, "All", StringComparison.OrdinalIgnoreCase))
                q = q.Where(x => x.Category == category);
        }

        return await q.OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<LocalExercise?> GetExerciseByLocalIdAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId && !x.IsDeleted);
    }

    public async Task UpsertExerciseAsync(LocalExercise e)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == e.LocalId);
        if (existing is null)
        {
            if (e.LocalId == Guid.Empty)
                e.LocalId = Guid.NewGuid();

            e.LastModifiedUtc = DateTime.UtcNow;
            e.SyncState = SyncState.Dirty;
            db.Exercises.Add(e);
        }
        else
        {
            existing.Name = e.Name;
            existing.Category = e.Category;
            existing.Notes = e.Notes;
            existing.IsDeleted = e.IsDeleted;

            existing.LastModifiedUtc = DateTime.UtcNow;
            existing.SyncState = SyncState.Dirty;
        }

        await db.SaveChangesAsync();
    }

    public async Task SoftDeleteExerciseAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var e = await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (e is null) return;

        e.IsDeleted = true;
        e.LastModifiedUtc = DateTime.UtcNow;
        e.SyncState = SyncState.Dirty;

        await db.SaveChangesAsync();
    }

    public async Task<List<LocalExercise>> GetDirtyExercisesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Exercises.Where(x => x.SyncState == SyncState.Dirty).ToListAsync();
    }

    public async Task MarkExerciseSyncedAsync(Guid localId, int? remoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var e = await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (e is null) return;

        if (remoteId.HasValue)
            e.RemoteId = remoteId;

        e.SyncState = SyncState.Synced;
        e.LastSyncedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task HardDeleteExerciseAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var e = await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (e is null) return;

        db.Exercises.Remove(e);
        await db.SaveChangesAsync();
    }

    public async Task MergeRemoteExercisesAsync(List<(int id, string name, string? category, string? notes)> remote)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var remoteIds = remote.Select(r => r.id).ToHashSet();

        foreach (var r in remote)
        {
            var local = await db.Exercises.FirstOrDefaultAsync(x => x.RemoteId == r.id);
            if (local is null)
            {
                db.Exercises.Add(new LocalExercise
                {
                    RemoteId = r.id,
                    Name = r.name,
                    Category = r.category ?? "",
                    Notes = r.notes,
                    IsDeleted = false,
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                });
            }
            else
            {
                if (local.SyncState == SyncState.Dirty)
                    continue;

                local.Name = r.name;
                local.Category = r.category ?? "";
                local.Notes = r.notes;
                local.IsDeleted = false;

                local.SyncState = SyncState.Synced;
                local.LastSyncedUtc = DateTime.UtcNow;
            }
        }

        var localsWithRemote = await db.Exercises.Where(x => x.RemoteId != null).ToListAsync();
        foreach (var l in localsWithRemote)
        {
            if (l.RemoteId is null) continue;
            if (remoteIds.Contains(l.RemoteId.Value)) continue;

            if (l.SyncState == SyncState.Dirty)
                continue;

            db.Exercises.Remove(l);
        }

        await db.SaveChangesAsync();
    }


    public async Task<List<LocalWorkout>> GetWorkoutsAsync(string? search = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<LocalWorkout> q = db.Workouts.AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            q = q.Where(x => x.Title.Contains(search));
        }

        return await q.OrderBy(x => x.Title).ToListAsync();
    }

    public async Task<LocalWorkout?> GetWorkoutByLocalIdAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId && !x.IsDeleted);
    }

    public async Task UpsertWorkoutAsync(LocalWorkout w)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == w.LocalId);
        if (existing is null)
        {
            if (w.LocalId == Guid.Empty)
                w.LocalId = Guid.NewGuid();

            w.LastModifiedUtc = DateTime.UtcNow;
            w.SyncState = SyncState.Dirty;
            db.Workouts.Add(w);
        }
        else
        {
            existing.Title = w.Title;
            existing.Notes = w.Notes;
            existing.IsDeleted = w.IsDeleted;

            existing.LastModifiedUtc = DateTime.UtcNow;
            existing.SyncState = SyncState.Dirty;
        }

        await db.SaveChangesAsync();
    }

    public async Task SoftDeleteWorkoutAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var w = await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (w is null) return;

        w.IsDeleted = true;
        w.LastModifiedUtc = DateTime.UtcNow;
        w.SyncState = SyncState.Dirty;

        await db.SaveChangesAsync();
    }

    public async Task<List<LocalWorkout>> GetDirtyWorkoutsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Workouts.Where(x => x.SyncState == SyncState.Dirty).ToListAsync();
    }

    public async Task MarkWorkoutSyncedAsync(Guid localId, int? remoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var w = await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (w is null) return;

        if (remoteId.HasValue)
            w.RemoteId = remoteId;

        w.SyncState = SyncState.Synced;
        w.LastSyncedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task HardDeleteWorkoutAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var w = await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (w is null) return;

        db.Workouts.Remove(w);
        await db.SaveChangesAsync();
    }

    public async Task MergeRemoteWorkoutsAsync(List<(int id, string title)> remote)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var remoteIds = remote.Select(r => r.id).ToHashSet();

        foreach (var r in remote)
        {
            var local = await db.Workouts.FirstOrDefaultAsync(x => x.RemoteId == r.id);
            if (local is null)
            {
                db.Workouts.Add(new LocalWorkout
                {
                    RemoteId = r.id,
                    Title = r.title,
                    Notes = null,
                    IsDeleted = false,
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                });
            }
            else
            {
                if (local.SyncState == SyncState.Dirty)
                    continue;

                local.Title = r.title;
                local.IsDeleted = false;

                local.SyncState = SyncState.Synced;
                local.LastSyncedUtc = DateTime.UtcNow;
            }
        }

        var localsWithRemote = await db.Workouts.Where(x => x.RemoteId != null).ToListAsync();
        foreach (var l in localsWithRemote)
        {
            if (l.RemoteId is null) continue;
            if (remoteIds.Contains(l.RemoteId.Value)) continue;

            if (l.SyncState == SyncState.Dirty)
                continue;

            db.Workouts.Remove(l);
        }

        await db.SaveChangesAsync();
    }

    public async Task<List<(LocalWorkoutExercise Link, LocalExercise Exercise)>> GetWorkoutExercisesAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var q =
            from link in db.WorkoutExercises.AsNoTracking()
            join ex in db.Exercises.AsNoTracking() on link.ExerciseLocalId equals ex.LocalId
            where link.WorkoutLocalId == workoutLocalId
                  && !link.IsDeleted
                  && !ex.IsDeleted
            orderby ex.Name
            select new { link, ex };

        var rows = await q.ToListAsync();
        return rows.Select(x => (x.link, x.ex)).ToList();
    }

    public async Task<List<LocalWorkoutExercise>> GetWorkoutExerciseLinksAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();
    }

    public async Task UpsertWorkoutExerciseAsync(LocalWorkoutExercise link)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.LocalId == link.LocalId);
        if (existing is null)
        {
            if (link.LocalId == Guid.Empty)
                link.LocalId = Guid.NewGuid();

            link.LastModifiedUtc = DateTime.UtcNow;
            link.SyncState = SyncState.Dirty;
            db.WorkoutExercises.Add(link);
        }
        else
        {
            existing.WorkoutLocalId = link.WorkoutLocalId;
            existing.ExerciseLocalId = link.ExerciseLocalId;
            existing.Reps = link.Reps;
            existing.WeightKg = link.WeightKg;
            existing.IsDeleted = link.IsDeleted;

            existing.LastModifiedUtc = DateTime.UtcNow;
            existing.SyncState = SyncState.Dirty;
        }

        await db.SaveChangesAsync();
    }

    public async Task SoftDeleteWorkoutExerciseAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var link = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (link is null) return;

        link.IsDeleted = true;
        link.LastModifiedUtc = DateTime.UtcNow;
        link.SyncState = SyncState.Dirty;

        await db.SaveChangesAsync();
    }

    public async Task<List<LocalWorkoutExercise>> GetDirtyWorkoutExercisesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkoutExercises.Where(x => x.SyncState == SyncState.Dirty).ToListAsync();
    }

    public async Task MarkWorkoutExerciseSyncedAsync(Guid localId, int? remoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var link = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (link is null) return;

        if (remoteId.HasValue)
            link.RemoteId = remoteId;

        link.SyncState = SyncState.Synced;
        link.LastSyncedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task HardDeleteWorkoutExerciseAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var link = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (link is null) return;

        db.WorkoutExercises.Remove(link);
        await db.SaveChangesAsync();
    }

   
    public async Task MergeRemoteWorkoutExercisesAsync(
        List<(int id, int workoutId, int exerciseId, int reps, double weightKg)> remote,
        Func<int, Guid?> mapWorkoutRemoteIdToLocalId,
        Func<int, Guid?> mapExerciseRemoteIdToLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var remoteIds = remote.Select(r => r.id).ToHashSet();

        foreach (var r in remote)
        {
            var workoutLocalId = mapWorkoutRemoteIdToLocalId(r.workoutId);
            var exerciseLocalId = mapExerciseRemoteIdToLocalId(r.exerciseId);

            if (workoutLocalId is null || exerciseLocalId is null)
                continue;

            var local = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.RemoteId == r.id);
            if (local is null)
            {
                db.WorkoutExercises.Add(new LocalWorkoutExercise
                {
                    RemoteId = r.id,
                    WorkoutLocalId = workoutLocalId.Value,
                    ExerciseLocalId = exerciseLocalId.Value,
                    Reps = r.reps,
                    WeightKg = r.weightKg,
                    IsDeleted = false,
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                });
            }
            else
            {
                if (local.SyncState == SyncState.Dirty)
                    continue;

                local.WorkoutLocalId = workoutLocalId.Value;
                local.ExerciseLocalId = exerciseLocalId.Value;
                local.Reps = r.reps;
                local.WeightKg = r.weightKg;
                local.IsDeleted = false;

                local.SyncState = SyncState.Synced;
                local.LastSyncedUtc = DateTime.UtcNow;
            }
        }

        var localsWithRemote = await db.WorkoutExercises.Where(x => x.RemoteId != null).ToListAsync();
        foreach (var l in localsWithRemote)
        {
            if (l.RemoteId is null) continue;
            if (remoteIds.Contains(l.RemoteId.Value)) continue;

            if (l.SyncState == SyncState.Dirty)
                continue;

            db.WorkoutExercises.Remove(l);
        }

        await db.SaveChangesAsync();
    }
}
