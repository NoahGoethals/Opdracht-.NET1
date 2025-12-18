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
                new LocalExercise { Name = "Bench Press", Category = "Chest", Notes = "Barbell", SyncState = SyncState.Synced, LastModifiedUtc = DateTime.UtcNow },
                new LocalExercise { Name = "Back Squat", Category = "Legs", Notes = "Depth focus", SyncState = SyncState.Synced, LastModifiedUtc = DateTime.UtcNow },
                new LocalExercise { Name = "Barbell Row", Category = "Back", Notes = "Strict form", SyncState = SyncState.Synced, LastModifiedUtc = DateTime.UtcNow }
            );

            db.Workouts.Add(new LocalWorkout { Title = "Full Body A", Notes = "Seed workout", SyncState = SyncState.Synced, LastModifiedUtc = DateTime.UtcNow });

            await db.SaveChangesAsync();
        }
    }

   
    public async Task<List<LocalExercise>> GetExercisesAsync(string? search = null, string? category = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var q = db.Exercises.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(x => x.Category == category);

        return await q
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<LocalExercise?> GetExerciseByLocalIdAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId);
    }

    public async Task UpsertExerciseAsync(LocalExercise e)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == e.LocalId);
        if (existing is null)
        {
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

        var q = db.Workouts.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Title.Contains(search));

        return await q
            .OrderBy(x => x.Title)
            .ToListAsync();
    }

    public async Task<LocalWorkout?> GetWorkoutByLocalIdAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId);
    }

    public async Task UpsertWorkoutAsync(LocalWorkout w)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == w.LocalId);
        if (existing is null)
        {
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
}
