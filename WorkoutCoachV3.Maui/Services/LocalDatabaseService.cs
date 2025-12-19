using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV3.Maui.Data;
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Services;

public class LocalDatabaseService
{
    private const int CurrentSchemaVersion = 4; // verhoog bij schema wijziging
    private const string SchemaPrefKey = "LocalDbSchemaVersion";
    private const string DbFileName = "workoutcoach.local.db3";

    private readonly IDbContextFactory<LocalAppDbContext> _dbFactory;

    public LocalDatabaseService(IDbContextFactory<LocalAppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public sealed record WorkoutExerciseDisplay(string Name, int Repetitions, double WeightKg);

    public sealed record WorkoutExerciseManageRow(
        Guid ExerciseLocalId,
        string Name,
        bool IsInWorkout,
        int Repetitions,
        double WeightKg
    );

    public async Task EnsureCreatedAndSeedAsync()
    {
        ResetDbIfSchemaChanged();

        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        await RepairWorkoutExercisesAsync(db);

        // Seed enkel als je OFFLINE bent en db leeg is (anders krijg je duplicates met web-data)
        var hasAnyExercises = await db.Exercises.AnyAsync();
        var hasAnyWorkouts = await db.Workouts.AnyAsync();

        var online = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess == Microsoft.Maui.Networking.NetworkAccess.Internet;

        if (!online && (!hasAnyExercises || !hasAnyWorkouts))
        {
            if (!hasAnyExercises)
            {
                db.Exercises.AddRange(
                    new LocalExercise { Name = "Bench Press", Category = "Chest", Notes = "Barbell", SyncState = SyncState.Dirty, LastModifiedUtc = DateTime.UtcNow },
                    new LocalExercise { Name = "Back Squat", Category = "Legs", Notes = "Depth focus", SyncState = SyncState.Dirty, LastModifiedUtc = DateTime.UtcNow },
                    new LocalExercise { Name = "Barbell Row", Category = "Back", Notes = "Strict form", SyncState = SyncState.Dirty, LastModifiedUtc = DateTime.UtcNow }
                );
            }

            if (!hasAnyWorkouts)
            {
                db.Workouts.Add(new LocalWorkout { Title = "Full Body A", Notes = "Offline seed", SyncState = SyncState.Dirty, LastModifiedUtc = DateTime.UtcNow });
            }

            await db.SaveChangesAsync();
        }
    }

    private static void ResetDbIfSchemaChanged()
    {
        try
        {
            var stored = Preferences.Get(SchemaPrefKey, -1);
            if (stored == CurrentSchemaVersion) return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
            Debug.WriteLine($"[DB] Schema {stored} -> {CurrentSchemaVersion}. Reset DB: {dbPath}");

            if (File.Exists(dbPath))
                File.Delete(dbPath);

            Preferences.Set(SchemaPrefKey, CurrentSchemaVersion);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DB] ResetDbIfSchemaChanged failed: " + ex);
        }
    }

    private static async Task RepairWorkoutExercisesAsync(LocalAppDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");

            // Unique index (defensief)
            await db.Database.ExecuteSqlRawAsync(@"
CREATE UNIQUE INDEX IF NOT EXISTS UX_WorkoutExercises_Workout_Exercise
ON WorkoutExercises(WorkoutLocalId, ExerciseLocalId);");

            // Remove duplicates (defensief)
            await db.Database.ExecuteSqlRawAsync(@"
DELETE FROM WorkoutExercises
WHERE rowid NOT IN (
    SELECT MIN(rowid)
    FROM WorkoutExercises
    GROUP BY WorkoutLocalId, ExerciseLocalId
);");
        }
        catch (Exception ex)
        {
            Debug.WriteLine("[DB][REPAIR] " + ex);
        }
    }

    // ---------------- EXERCISES ----------------

    public async Task<List<LocalExercise>> GetExercisesAsync(string? search = null, string? category = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.Exercises.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(x => x.Category == category);

        return await q.OrderBy(x => x.Name).ToListAsync();
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

        var links = await db.WorkoutExercises.Where(x => x.ExerciseLocalId == localId).ToListAsync();
        db.WorkoutExercises.RemoveRange(links);

        var e = await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (e is not null)
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

        // remove vanished remote
        var localsWithRemote = await db.Exercises.Where(x => x.RemoteId != null).ToListAsync();
        foreach (var l in localsWithRemote)
        {
            if (l.RemoteId is null) continue;
            if (remoteIds.Contains(l.RemoteId.Value)) continue;
            if (l.SyncState == SyncState.Dirty) continue;

            db.Exercises.Remove(l);
        }

        await db.SaveChangesAsync();
    }

    // ---------------- WORKOUTS ----------------

    public async Task<List<LocalWorkout>> GetWorkoutsAsync(string? search = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.Workouts.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Title.Contains(search));

        return await q.OrderBy(x => x.Title).ToListAsync();
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

        var links = await db.WorkoutExercises.Where(x => x.WorkoutLocalId == localId).ToListAsync();
        db.WorkoutExercises.RemoveRange(links);

        var w = await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (w is not null)
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
            if (l.SyncState == SyncState.Dirty) continue;

            db.Workouts.Remove(l);
        }

        await db.SaveChangesAsync();
    }

    // ---------------- JOIN: WORKOUT EXERCISES ----------------

    public async Task<List<WorkoutExerciseDisplay>> GetWorkoutExercisesAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var links = await db.WorkoutExercises
            .Where(x => !x.IsDeleted && x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        var exIds = links.Select(x => x.ExerciseLocalId).Distinct().ToList();

        var exMap = await db.Exercises
            .Where(e => !e.IsDeleted && exIds.Contains(e.LocalId))
            .ToDictionaryAsync(e => e.LocalId, e => e.Name);

        return links
            .Where(l => exMap.ContainsKey(l.ExerciseLocalId))
            .Select(l => new WorkoutExerciseDisplay(exMap[l.ExerciseLocalId], l.Repetitions, l.WeightKg))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<List<WorkoutExerciseManageRow>> GetWorkoutExerciseManageRowsAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var allExercises = await db.Exercises
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var inWorkout = await db.WorkoutExercises
            .Where(x => !x.IsDeleted && x.WorkoutLocalId == workoutLocalId)
            .ToDictionaryAsync(x => x.ExerciseLocalId, x => x);

        var result = new List<WorkoutExerciseManageRow>(allExercises.Count);

        foreach (var ex in allExercises)
        {
            if (inWorkout.TryGetValue(ex.LocalId, out var link))
                result.Add(new WorkoutExerciseManageRow(ex.LocalId, ex.Name, true, link.Repetitions, link.WeightKg));
            else
                result.Add(new WorkoutExerciseManageRow(ex.LocalId, ex.Name, false, 0, 0));
        }

        return result;
    }

    public async Task SaveWorkoutExercisesAsync(
        Guid workoutLocalId,
        List<(Guid ExerciseLocalId, bool IsInWorkout, int Repetitions, double WeightKg)> data)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var normalized = data
                .GroupBy(x => x.ExerciseLocalId)
                .Select(g => g.Last())
                .ToList();

            var existing = await db.WorkoutExercises
                .Where(x => x.WorkoutLocalId == workoutLocalId)
                .ToListAsync();

            var map = existing.ToDictionary(x => x.ExerciseLocalId, x => x);

            foreach (var row in normalized)
            {
                var reps = Math.Max(0, row.Repetitions);
                var weight = Math.Max(0, row.WeightKg);

                if (!row.IsInWorkout)
                {
                    if (map.TryGetValue(row.ExerciseLocalId, out var link))
                    {
                        link.IsDeleted = true;
                        link.SyncState = SyncState.Dirty;
                        link.LastModifiedUtc = DateTime.UtcNow;
                    }
                    continue;
                }

                if (map.TryGetValue(row.ExerciseLocalId, out var existingLink))
                {
                    existingLink.Repetitions = reps;
                    existingLink.WeightKg = weight;
                    existingLink.IsDeleted = false;
                    existingLink.SyncState = SyncState.Dirty;
                    existingLink.LastModifiedUtc = DateTime.UtcNow;
                }
                else
                {
                    db.WorkoutExercises.Add(new LocalWorkoutExercise
                    {
                        WorkoutLocalId = workoutLocalId,
                        ExerciseLocalId = row.ExerciseLocalId,
                        Repetitions = reps,
                        WeightKg = weight,
                        IsDeleted = false,
                        SyncState = SyncState.Dirty,
                        LastModifiedUtc = DateTime.UtcNow
                    });
                }
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException(BuildDbUpdateDetails(ex), ex);
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    // ---- helpers voor SyncService ----

    public async Task<List<LocalWorkoutExercise>> GetDirtyWorkoutExercisesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkoutExercises
            .Where(x => x.SyncState == SyncState.Dirty)
            .ToListAsync();
    }

    public async Task<List<LocalWorkoutExercise>> GetWorkoutExercisesAllStatesAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();
    }

    public async Task MarkWorkoutExercisesSyncedAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var rows = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        foreach (var r in rows)
        {
            r.SyncState = SyncState.Synced;
            r.LastSyncedUtc = DateTime.UtcNow;
        }

        // optioneel: hard delete soft-deleted rows na succesvolle push
        var toHardDelete = rows.Where(x => x.IsDeleted).ToList();
        db.WorkoutExercises.RemoveRange(toHardDelete);

        await db.SaveChangesAsync();
    }

    public async Task ReplaceWorkoutExercisesFromRemoteAsync(
        Guid workoutLocalId,
        List<(Guid ExerciseLocalId, int Repetitions, double WeightKg)> remote)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        // mark all existing as deleted (then re-add/update)
        var existing = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        foreach (var e in existing)
        {
            e.IsDeleted = true;
            e.SyncState = SyncState.Synced;
            e.LastSyncedUtc = DateTime.UtcNow;
        }

        var map = existing.ToDictionary(x => x.ExerciseLocalId, x => x);

        foreach (var r in remote)
        {
            if (map.TryGetValue(r.ExerciseLocalId, out var link))
            {
                link.IsDeleted = false;
                link.Repetitions = Math.Max(0, r.Repetitions);
                link.WeightKg = Math.Max(0, r.WeightKg);
                link.SyncState = SyncState.Synced;
                link.LastSyncedUtc = DateTime.UtcNow;
            }
            else
            {
                db.WorkoutExercises.Add(new LocalWorkoutExercise
                {
                    WorkoutLocalId = workoutLocalId,
                    ExerciseLocalId = r.ExerciseLocalId,
                    Repetitions = Math.Max(0, r.Repetitions),
                    WeightKg = Math.Max(0, r.WeightKg),
                    IsDeleted = false,
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = DateTime.UtcNow,
                    LastSyncedUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static string BuildDbUpdateDetails(DbUpdateException ex)
    {
        if (ex.InnerException is SqliteException se)
        {
            return
                "SQLite fout bij opslaan.\n" +
                $"Message: {se.Message}\n" +
                $"SqliteErrorCode: {se.SqliteErrorCode}\n" +
                $"HResult: {se.HResult}\n" +
                "Hint: meestal schema mismatch (oude db3). Verhoog schema versie of verwijder db3.";
        }

        return $"Database fout bij opslaan.\nMessage: {ex.Message}\nInner: {ex.InnerException?.Message}";
    }
}
