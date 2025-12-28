using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Linq;
using WorkoutCoachV3.Maui.Data;
using WorkoutCoachV3.Maui.Data.LocalEntities;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public class LocalDatabaseService
{
    // Lokale DB schema versie: bij wijziging wordt DB-file verwijderd en opnieuw aangemaakt.
    private const int CurrentSchemaVersion = 5;
    private const string SchemaPrefKey = "LocalDbSchemaVersion";
    private const string DbFileName = "workoutcoach.local.db3";

    // Factory om DbContext per operatie te maken (veilig voor async/threads).
    private readonly IDbContextFactory<LocalAppDbContext> _dbFactory;

    public LocalDatabaseService(IDbContextFactory<LocalAppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    // Prefix om te onthouden welke workouts gebruikt werden om een session te genereren.
    public const string SessionSourceWorkoutsNotesPrefix = "__src_workouts:";

    // Bouwt een notes-string op met workout LocalIds zodat je later de bron kan reconstrueren.
    private static string BuildSessionSourceWorkoutsNotes(IEnumerable<Guid> workoutIds)
    {
        var ids = workoutIds?.ToList() ?? new List<Guid>();
        return SessionSourceWorkoutsNotesPrefix + string.Join(";", ids);
    }

    // Display record: enkel wat je op de Workout detail UI wil tonen.
    public sealed record WorkoutExerciseDisplay(string Name, int Repetitions, double WeightKg);

    // Manage-row: combineert exercise + selectie + reps/weight voor de manage UI.
    public sealed record WorkoutExerciseManageRow(
        Guid ExerciseLocalId,
        string Name,
        bool IsInWorkout,
        int Repetitions,
        double WeightKg
    );

    // Compacte lijstweergave voor SessionsPage (id + titel + datum).
    public record SessionListDisplay(Guid SessionLocalId, string Title, DateTime Date);

    // UI display voor sets binnen een sessie (setnr + exercise + reps/weight).
    public record SessionSetDisplay(int SetNumber, string ExerciseName, int Reps, double Weight);

    // Stats header-kerncijfers (tellingen + totale volume).
    public sealed record StatsSummary(int Sessions, int Sets, int TotalReps, double TotalVolumeKg);

    // Stats rij per oefening (sets/reps/volume/max).
    public sealed record ExerciseStatsRow(
        Guid ExerciseLocalId,
        string ExerciseName,
        int Sets,
        int Reps,
        double VolumeKg,
        double MaxKg);


    // Zorgt dat DB bestaat en zet offline seed data als er geen internet is.
    public async Task EnsureCreatedAndSeedAsync()
    {
        ResetDbIfSchemaChanged();

        await using var db = await _dbFactory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();

        await RepairWorkoutExercisesAsync(db);

        var hasAnyExercises = await db.Exercises.AnyAsync();
        var hasAnyWorkouts = await db.Workouts.AnyAsync();

        var online = Microsoft.Maui.Networking.Connectivity.Current.NetworkAccess ==
                     Microsoft.Maui.Networking.NetworkAccess.Internet;

        if (!online && (!hasAnyExercises || !hasAnyWorkouts))
        {
            if (!hasAnyExercises)
            {
                // Offline seed: enkele basis oefeningen zodat de app bruikbaar blijft zonder API.
                db.Exercises.AddRange(
                    new LocalExercise
                    {
                        Name = "Bench Press",
                        Category = "Chest",
                        Notes = "Barbell",
                        SyncState = SyncState.Dirty,
                        LastModifiedUtc = DateTime.UtcNow
                    },
                    new LocalExercise
                    {
                        Name = "Back Squat",
                        Category = "Legs",
                        Notes = "Depth focus",
                        SyncState = SyncState.Dirty,
                        LastModifiedUtc = DateTime.UtcNow
                    },
                    new LocalExercise
                    {
                        Name = "Barbell Row",
                        Category = "Back",
                        Notes = "Strict form",
                        SyncState = SyncState.Dirty,
                        LastModifiedUtc = DateTime.UtcNow
                    }
                );
            }

            if (!hasAnyWorkouts)
            {
                // Offline seed: minstens één workout zodat session-from-workouts werkt.
                db.Workouts.Add(new LocalWorkout
                {
                    Title = "Full Body A",
                    Notes = "Offline seed",
                    SyncState = SyncState.Dirty,
                    LastModifiedUtc = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync();
        }
    }

    // Bij schema change: delete DB file zodat EF opnieuw tables/indexes maakt.
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
            // Best effort: falen mag de app niet blokkeren.
            Debug.WriteLine("[DB] ResetDbIfSchemaChanged failed: " + ex);
        }
    }

    // Repair: forceer foreign_keys + fix unieke index + verwijder duplicate links.
    private static async Task RepairWorkoutExercisesAsync(LocalAppDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");

            await db.Database.ExecuteSqlRawAsync(@"
CREATE UNIQUE INDEX IF NOT EXISTS UX_WorkoutExercises_Workout_Exercise
ON WorkoutExercises(WorkoutLocalId, ExerciseLocalId);");

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
            // Repair errors loggen: DB kan nog werken, maar zonder cleanup.
            Debug.WriteLine("[DB][REPAIR] " + ex);
        }
    }

    // Maakt een leesbare error-string met inner exception details (SQLite/EF).
    private static string BuildDbUpdateDetails(DbUpdateException ex)
    {
        var inner = ex.InnerException?.Message ?? "(no inner exception)";
        return $"{ex.Message}\nINNER: {inner}";
    }


    // Exercises lijst: filter op search + category en soft-delete respecteren.
    public async Task<List<LocalExercise>> GetExercisesAsync(string? search = null, string? category = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        IQueryable<LocalExercise> q = db.Exercises.Where(e => !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
        {
            // Case-insensitive search (via ToLower) om eenvoudig lokaal te filteren.
            var s = search.Trim().ToLowerInvariant();
            q = q.Where(x => x.Name != null && x.Name.ToLower()!.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            q = q.Where(x => x.Category == category);
        }

        return await q.OrderBy(x => x.Name).ToListAsync();
    }


    // Haalt één exercise entity op via LocalId.
    public async Task<LocalExercise?> GetExerciseByLocalIdAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Exercises.FirstOrDefaultAsync(x => x.LocalId == localId);
    }

    // Upsert: insert als nieuw, anders update fields en markeer Dirty voor sync.
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

    // Soft delete: markeer IsDeleted + Dirty zodat push delete kan gebeuren.
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

    // Geeft alle Dirty oefeningen terug (incl. deletes) voor sync push.
    public async Task<List<LocalExercise>> GetDirtyExercisesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Exercises.Where(x => x.SyncState == SyncState.Dirty).ToListAsync();
    }

    // Markeer oefening als Synced en zet eventueel RemoteId na create.
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

    // Hard delete: verwijder ook links zodat FK/unique issues vermeden worden.
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

    // Merge remote -> local: update alleen als local niet Dirty is (offline edits winnen).
    public async Task MergeRemoteExercisesAsync(List<(int id, string name, string? category, string? notes)> remote)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var remoteIds = remote.Select(r => r.id).ToHashSet();

        foreach (var r in remote)
        {
            var local = await db.Exercises.FirstOrDefaultAsync(x => x.RemoteId == r.id);
            if (local is null)
            {
                // Nieuwe remote oefening bestaat nog niet lokaal -> add als Synced.
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

        // Verwijder lokale items die remote niet meer bestaan (en niet Dirty zijn).
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


    // Workouts lijst: simple title search + soft-delete respecteren.
    public async Task<List<LocalWorkout>> GetWorkoutsAsync(string? search = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.Workouts.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Title.Contains(search));

        return await q.OrderBy(x => x.Title).ToListAsync();
    }

    // Haalt één workout op via LocalId.
    public async Task<LocalWorkout?> GetWorkoutByLocalIdAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Workouts.FirstOrDefaultAsync(x => x.LocalId == localId);
    }

    // Upsert workout: markeer Dirty zodat sync push update/create doet.
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

    // Soft delete workout: laat sync delete uitvoeren, maar behoud data tot sync klaar is.
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

    // Geeft Dirty workouts terug voor sync push.
    public async Task<List<LocalWorkout>> GetDirtyWorkoutsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Workouts.Where(x => x.SyncState == SyncState.Dirty).ToListAsync();
    }

    // Markeer workout als Synced en zet eventueel RemoteId na create.
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

    // Hard delete workout + links zodat DB consistent blijft.
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

    // Merge remote workouts -> local, zonder Dirty locals te overschrijven.
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

        // Verwijder lokale workouts die remote niet meer bestaan (en niet Dirty zijn).
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


    // Geeft alle links terug, inclusief deleted, om bulk replace/push correct te kunnen doen.
    public async Task<List<LocalWorkoutExercise>> GetWorkoutExercisesAllStatesAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();
    }

    // Alias naar display helper (voor UI).
    public async Task<List<WorkoutExerciseDisplay>> GetWorkoutExercisesAsync(Guid workoutLocalId)
        => await GetWorkoutExercisesDisplayAsync(workoutLocalId);

    // Haalt workout-exercises op met name mapping (LocalId -> Name).
    public async Task<List<WorkoutExerciseDisplay>> GetWorkoutExercisesDisplayAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var links = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId && !x.IsDeleted)
            .ToListAsync();

        var exMap = await db.Exercises
            .Where(e => !e.IsDeleted)
            .ToDictionaryAsync(e => e.LocalId, e => e.Name);

        return links
            .Where(l => exMap.ContainsKey(l.ExerciseLocalId))
            .Select(l => new WorkoutExerciseDisplay(
                Name: exMap[l.ExerciseLocalId],
                Repetitions: l.Repetitions,
                WeightKg: l.WeightKg
            ))
            .OrderBy(x => x.Name)
            .ToList();
    }

    // Bouwt de manage rows: elke exercise + bestaande link status (incl. deleted links).
    public async Task<List<WorkoutExerciseManageRow>> GetWorkoutExerciseManageRowsAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var exercises = await db.Exercises
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var links = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        var linkByExercise = links.ToDictionary(x => x.ExerciseLocalId, x => x);

        return exercises.Select(e =>
        {
            if (linkByExercise.TryGetValue(e.LocalId, out var link))
            {
                return new WorkoutExerciseManageRow(
                    ExerciseLocalId: e.LocalId,
                    Name: e.Name,
                    IsInWorkout: !link.IsDeleted,
                    Repetitions: link.Repetitions,
                    WeightKg: link.WeightKg
                );
            }

            return new WorkoutExerciseManageRow(
                ExerciseLocalId: e.LocalId,
                Name: e.Name,
                IsInWorkout: false,
                Repetitions: 0,
                WeightKg: 0.0
            );
        }).ToList();
    }

    // Helper: zet tuple input om naar manage rows en hergebruik de apply methode.
    public async Task SaveWorkoutExercisesAsync(
        Guid workoutLocalId,
        List<(Guid ExerciseLocalId, bool IsInWorkout, int Repetitions, double WeightKg)> rows)
    {
        var mapped = rows.Select(r => new WorkoutExerciseManageRow(
            ExerciseLocalId: r.ExerciseLocalId,
            Name: "",
            IsInWorkout: r.IsInWorkout,
            Repetitions: r.Repetitions,
            WeightKg: r.WeightKg
        )).ToList();

        await ApplyWorkoutExerciseSelectionsAsync(workoutLocalId, mapped);
    }

    // Apply: update bestaande links of create nieuwe, markeer Dirty voor sync push.
    public async Task ApplyWorkoutExerciseSelectionsAsync(Guid workoutLocalId, List<WorkoutExerciseManageRow> rows)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existingLinks = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        var byExercise = existingLinks.ToDictionary(x => x.ExerciseLocalId, x => x);

        foreach (var row in rows)
        {
            if (byExercise.TryGetValue(row.ExerciseLocalId, out var link))
            {
                link.IsDeleted = !row.IsInWorkout;
                link.Repetitions = Math.Max(0, row.Repetitions);
                link.WeightKg = Math.Max(0.0, row.WeightKg);

                link.SyncState = SyncState.Dirty;
                link.LastModifiedUtc = DateTime.UtcNow;
            }
            else
            {
                if (!row.IsInWorkout) continue;

                db.WorkoutExercises.Add(new LocalWorkoutExercise
                {
                    WorkoutLocalId = workoutLocalId,
                    ExerciseLocalId = row.ExerciseLocalId,
                    Repetitions = Math.Max(0, row.Repetitions),
                    WeightKg = Math.Max(0.0, row.WeightKg),
                    IsDeleted = false,
                    SyncState = SyncState.Dirty,
                    LastModifiedUtc = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }

    // Geeft alle Dirty links terug (voor bulk push per workout).
    public async Task<List<LocalWorkoutExercise>> GetDirtyWorkoutExercisesAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.WorkoutExercises.Where(x => x.SyncState == SyncState.Dirty).ToListAsync();
    }

    // Markeer één link als Synced (handig als je per record zou syncen).
    public async Task MarkWorkoutExerciseSyncedAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var we = await db.WorkoutExercises.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (we is null) return;

        we.SyncState = SyncState.Synced;
        we.LastSyncedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    // Markeer alle links van een workout als Synced na bulk replace.
    public async Task MarkWorkoutExercisesSyncedAsync(Guid workoutLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var links = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var l in links)
        {
            l.SyncState = SyncState.Synced;
            l.LastSyncedUtc = now;
        }

        await db.SaveChangesAsync();
    }

    // Vervangt lokale links door remote set (pull): start met remove-all en voeg remote terug toe.
    public async Task ReplaceWorkoutExercisesFromRemoteAsync(
        Guid workoutLocalId,
        List<(Guid ExerciseLocalId, int Repetitions, double WeightKg)> remote)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.WorkoutExercises
            .Where(x => x.WorkoutLocalId == workoutLocalId)
            .ToListAsync();

        db.WorkoutExercises.RemoveRange(existing);
        await db.SaveChangesAsync();

        foreach (var r in remote)
        {
            db.WorkoutExercises.Add(new LocalWorkoutExercise
            {
                WorkoutLocalId = workoutLocalId,
                ExerciseLocalId = r.ExerciseLocalId,
                Repetitions = Math.Max(0, r.Repetitions),
                WeightKg = Math.Max(0.0, r.WeightKg),
                IsDeleted = false,
                SyncState = SyncState.Synced,
                LastModifiedUtc = DateTime.UtcNow,
                LastSyncedUtc = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }


    // Sessions lijst voor UI: compact display met sortering op datum desc.
    public async Task<List<SessionListDisplay>> GetSessionsAsync(string? search = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var q = db.Sessions.Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(x => x.Title.Contains(search));

        var list = await q
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.Title)
            .ToListAsync();

        return list.Select(x => new SessionListDisplay(x.LocalId, x.Title, x.Date)).ToList();
    }

    // Haalt een session entity op via LocalId.
    public async Task<LocalSession?> GetSessionByLocalIdAsync(Guid sessionLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Sessions.FirstOrDefaultAsync(x => x.LocalId == sessionLocalId);
    }

    // Upsert session: update basisvelden en markeer Dirty voor sync.
    public async Task UpsertSessionAsync(LocalSession s)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var existing = await db.Sessions.FirstOrDefaultAsync(x => x.LocalId == s.LocalId);
        if (existing is null)
        {
            s.LastModifiedUtc = DateTime.UtcNow;
            s.SyncState = SyncState.Dirty;
            db.Sessions.Add(s);
        }
        else
        {
            existing.Title = s.Title;
            existing.Date = s.Date;
            existing.Description = s.Description;
            existing.Notes = s.Notes;

            existing.IsDeleted = s.IsDeleted;

            existing.LastModifiedUtc = DateTime.UtcNow;
            existing.SyncState = SyncState.Dirty;
        }

        await db.SaveChangesAsync();
    }

    // Soft delete session: markeer Dirty zodat API delete kan gebeuren.
    public async Task SoftDeleteSessionAsync(Guid sessionLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var s = await db.Sessions.FirstOrDefaultAsync(x => x.LocalId == sessionLocalId);
        if (s is null) return;

        s.IsDeleted = true;
        s.LastModifiedUtc = DateTime.UtcNow;
        s.SyncState = SyncState.Dirty;

        await db.SaveChangesAsync();
    }

    // Haalt set display op voor session detail page, met mapping naar exercise names.
    public async Task<List<SessionSetDisplay>> GetSessionSetsAsync(Guid sessionLocalId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var sets = await db.SessionSets
            .Where(s => s.SessionLocalId == sessionLocalId && !s.IsDeleted)
            .OrderBy(s => s.ExerciseLocalId)
            .ThenBy(s => s.SetNumber)
            .ToListAsync();

        var exMap = await db.Exercises
            .Where(e => !e.IsDeleted)
            .ToDictionaryAsync(e => e.LocalId, e => e.Name);

        return sets
            .Where(s => exMap.ContainsKey(s.ExerciseLocalId))
            .Select(s => new SessionSetDisplay(
                s.SetNumber,
                exMap[s.ExerciseLocalId],
                s.Reps,
                s.Weight
            ))
            .OrderBy(x => x.ExerciseName)
            .ThenBy(x => x.SetNumber)
            .ToList();
    }

    // Maakt een session aan op basis van geselecteerde workouts (kopieert exercises naar sets).
    public async Task<Guid> CreateSessionFromWorkoutsAsync(
        string title,
        DateTime date,
        string? description,
        List<Guid> selectedWorkoutLocalIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var session = new LocalSession
            {
                LocalId = Guid.NewGuid(),
                Title = title.Trim(),
                Date = date,
                Description = description,
                Notes = BuildSessionSourceWorkoutsNotes(selectedWorkoutLocalIds),
                IsDeleted = false,
                SyncState = SyncState.Dirty,
                LastModifiedUtc = DateTime.UtcNow
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var workoutExercises = await db.WorkoutExercises
                .Where(x => !x.IsDeleted && selectedWorkoutLocalIds.Contains(x.WorkoutLocalId))
                .OrderBy(x => x.WorkoutLocalId)
                .ThenBy(x => x.ExerciseLocalId)
                .ToListAsync();

            var nextSetNr = new Dictionary<Guid, int>();

            foreach (var we in workoutExercises)
            {
                // SetNumber per exercise wordt opgehoogd zodat je meerdere sets krijgt bij dezelfde oefening.
                if (!nextSetNr.TryGetValue(we.ExerciseLocalId, out var nr))
                    nr = 1;

                db.SessionSets.Add(new LocalSessionSet
                {
                    LocalId = Guid.NewGuid(),
                    SessionLocalId = session.LocalId,
                    ExerciseLocalId = we.ExerciseLocalId,
                    SetNumber = nr,
                    Reps = Math.Max(0, we.Repetitions),
                    Weight = Math.Max(0.0, we.WeightKg),
                    Completed = false,
                    IsDeleted = false,
                    SyncState = SyncState.Dirty,
                    LastModifiedUtc = DateTime.UtcNow
                });

                nextSetNr[we.ExerciseLocalId] = nr + 1;
            }

            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return session.LocalId;
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

    // Update session opnieuw op basis van workouts: vervangt alle sets (remove + rebuild).
    public async Task UpdateSessionFromWorkoutsAsync(
        Guid sessionLocalId,
        string title,
        DateTime date,
        string? description,
        List<Guid> selectedWorkoutLocalIds)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        try
        {
            var session = await db.Sessions.FirstOrDefaultAsync(x => x.LocalId == sessionLocalId);
            if (session is null)
                throw new InvalidOperationException("Session not found.");

            session.Title = title.Trim();
            session.Date = date;
            session.Description = description;
            session.Notes = BuildSessionSourceWorkoutsNotes(selectedWorkoutLocalIds);
            session.IsDeleted = false;
            session.SyncState = SyncState.Dirty;
            session.LastModifiedUtc = DateTime.UtcNow;

            var existingSets = await db.SessionSets
                .Where(x => x.SessionLocalId == sessionLocalId)
                .ToListAsync();

            if (existingSets.Count > 0)
                db.SessionSets.RemoveRange(existingSets);

            await db.SaveChangesAsync();

            var workoutExercises = await db.WorkoutExercises
                .Where(x => !x.IsDeleted && selectedWorkoutLocalIds.Contains(x.WorkoutLocalId))
                .OrderBy(x => x.WorkoutLocalId)
                .ThenBy(x => x.ExerciseLocalId)
                .ToListAsync();

            var nextSetNr = new Dictionary<Guid, int>();

            foreach (var we in workoutExercises)
            {
                if (!nextSetNr.TryGetValue(we.ExerciseLocalId, out var nr))
                    nr = 1;

                db.SessionSets.Add(new LocalSessionSet
                {
                    LocalId = Guid.NewGuid(),
                    SessionLocalId = session.LocalId,
                    ExerciseLocalId = we.ExerciseLocalId,
                    SetNumber = nr,
                    Reps = Math.Max(0, we.Repetitions),
                    Weight = Math.Max(0.0, we.WeightKg),
                    Completed = false,
                    IsDeleted = false,
                    SyncState = SyncState.Dirty,
                    LastModifiedUtc = DateTime.UtcNow
                });

                nextSetNr[we.ExerciseLocalId] = nr + 1;
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

    // Dirty sessions: ook sessions met dirty sets of zonder RemoteId moeten gepusht worden.
    public async Task<List<LocalSession>> GetDirtySessionsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var dirtySessionIdsFromSets = await db.SessionSets
            .Where(s => s.SyncState == SyncState.Dirty)
            .Select(s => s.SessionLocalId)
            .Distinct()
            .ToListAsync();

        return await db.Sessions
            .Where(s =>
                s.SyncState == SyncState.Dirty ||
                s.IsDeleted ||
                !s.RemoteId.HasValue ||
                dirtySessionIdsFromSets.Contains(s.LocalId))
            .ToListAsync();
    }

    // Haalt set entities op (optioneel inclusief deleted) voor sync payload build.
    public async Task<List<LocalSessionSet>> GetSessionSetsEntitiesAsync(Guid sessionLocalId, bool includeDeleted = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var q = db.SessionSets.Where(x => x.SessionLocalId == sessionLocalId);
        if (!includeDeleted) q = q.Where(x => !x.IsDeleted);
        return await q.OrderBy(x => x.ExerciseLocalId).ThenBy(x => x.SetNumber).ToListAsync();
    }

    // Hard delete session: verwijder eerst sets, dan session.
    public async Task HardDeleteSessionAsync(Guid localId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var sets = await db.SessionSets.Where(x => x.SessionLocalId == localId).ToListAsync();
        if (sets.Count > 0) db.SessionSets.RemoveRange(sets);

        var entity = await db.Sessions.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (entity != null)
        {
            db.Sessions.Remove(entity);
            await db.SaveChangesAsync();
        }
        else if (sets.Count > 0)
        {
            await db.SaveChangesAsync();
        }
    }

    // Mark session + alle sets als Synced na succesvolle push.
    public async Task MarkSessionSyncedAsync(Guid localId, int? remoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var s = await db.Sessions.FirstOrDefaultAsync(x => x.LocalId == localId);
        if (s is null) return;

        if (remoteId.HasValue) s.RemoteId = remoteId.Value;

        s.SyncState = SyncState.Synced;
        s.LastSyncedUtc = DateTime.UtcNow;

        var sets = await db.SessionSets.Where(x => x.SessionLocalId == localId).ToListAsync();
        foreach (var set in sets)
        {
            set.SyncState = SyncState.Synced;
            set.LastSyncedUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    // Merge remote sessions + sets: update alleen als local session niet Dirty is.
    public async Task MergeRemoteSessionsAsync(List<SessionDto> remote)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var now = DateTime.UtcNow;

        var remoteIds = remote.Select(r => r.Id).ToHashSet();

        var exMap = await db.Exercises
            .Where(e => e.RemoteId != null && !e.IsDeleted)
            .ToDictionaryAsync(e => e.RemoteId!.Value, e => e.LocalId);

        foreach (var r in remote)
        {
            var local = await db.Sessions.FirstOrDefaultAsync(x => x.RemoteId == r.Id);

            if (local is null)
            {
                local = new LocalSession
                {
                    LocalId = Guid.NewGuid(),
                    RemoteId = r.Id,
                    Title = r.Title,
                    Date = r.Date,
                    Description = r.Description,
                    Notes = null,
                    IsDeleted = false,
                    SyncState = SyncState.Synced,
                    LastModifiedUtc = now,
                    LastSyncedUtc = now
                };

                db.Sessions.Add(local);
                await db.SaveChangesAsync();
            }
            else
            {
                if (local.SyncState == SyncState.Dirty)
                    continue;

                local.Title = r.Title;
                local.Date = r.Date;
                local.Description = r.Description;
                local.IsDeleted = false;
                local.SyncState = SyncState.Synced;
                local.LastSyncedUtc = now;
            }

            if (local.SyncState == SyncState.Dirty)
                continue;

            var existingSets = await db.SessionSets.Where(x => x.SessionLocalId == local.LocalId).ToListAsync();
            if (existingSets.Count > 0)
                db.SessionSets.RemoveRange(existingSets);

            if (r.Sets is not null)
            {
                // Remote sets worden gemapt naar lokale ExerciseLocalId via RemoteId lookup.
                foreach (var rs in r.Sets)
                {
                    if (!exMap.TryGetValue(rs.ExerciseId, out var localExerciseId))
                        continue;

                    db.SessionSets.Add(new LocalSessionSet
                    {
                        LocalId = Guid.NewGuid(),
                        SessionLocalId = local.LocalId,
                        ExerciseLocalId = localExerciseId,
                        SetNumber = Math.Max(1, rs.SetNumber),
                        Reps = Math.Max(0, rs.Reps),
                        Weight = Math.Max(0.0, rs.Weight),
                        IsDeleted = false,
                        SyncState = SyncState.Synced,
                        LastModifiedUtc = now,
                        LastSyncedUtc = now
                    });
                }
            }
        }

        // Verwijder lokale sessions die remote niet meer bestaan (en niet Dirty zijn).
        var localsWithRemote = await db.Sessions.Where(x => x.RemoteId != null).ToListAsync();
        foreach (var l in localsWithRemote)
        {
            if (l.RemoteId is null) continue;
            if (remoteIds.Contains(l.RemoteId.Value)) continue;
            if (l.SyncState == SyncState.Dirty) continue;

            var sets = await db.SessionSets.Where(x => x.SessionLocalId == l.LocalId).ToListAsync();
            if (sets.Count > 0) db.SessionSets.RemoveRange(sets);

            db.Sessions.Remove(l);
        }

        await db.SaveChangesAsync();
    }


    // Stats query: join SessionSets + Sessions en filter op exercise en datum range.
    public async Task<(StatsSummary Summary, List<ExerciseStatsRow> TopExercises)> GetStatsAsync(
        Guid? exerciseLocalId,
        DateTime? from,
        DateTime? to,
        int takeTop = 50)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var q =
            from set in db.SessionSets
            join session in db.Sessions on set.SessionLocalId equals session.LocalId
            where !set.IsDeleted && !session.IsDeleted
            select new
            {
                SessionLocalId = session.LocalId,
                SessionDate = session.Date,
                set.ExerciseLocalId,
                set.Reps,
                Weight = set.Weight
            };

        if (exerciseLocalId.HasValue)
            q = q.Where(x => x.ExerciseLocalId == exerciseLocalId.Value);

        if (from.HasValue)
        {
            var f = from.Value.Date;
            q = q.Where(x => x.SessionDate.Date >= f);
        }

        if (to.HasValue)
        {
            var t = to.Value.Date;
            q = q.Where(x => x.SessionDate.Date <= t);
        }

        // Summary: sessions distinct, sets count, reps sum, volume sum(reps*weight).
        var sessions = await q.Select(x => x.SessionLocalId).Distinct().CountAsync();
        var sets = await q.CountAsync();
        var totalReps = await q.SumAsync(x => (int?)x.Reps) ?? 0;
        var totalVolume = await q.SumAsync(x => (double?)(x.Reps * x.Weight)) ?? 0.0;

        // Map local exercise id -> name voor output rows.
        var exerciseNames = await db.Exercises
            .Where(e => !e.IsDeleted)
            .ToDictionaryAsync(e => e.LocalId, e => e.Name);

        // Top: groepeer per exercise en sorteer op volume, dan op max gewicht.
        var top = await q
            .GroupBy(x => x.ExerciseLocalId)
            .Select(g => new
            {
                ExerciseLocalId = g.Key,
                Sets = g.Count(),
                Reps = g.Sum(x => x.Reps),
                Volume = g.Sum(x => x.Reps * x.Weight),
                MaxKg = g.Max(x => x.Weight)
            })
            .OrderByDescending(x => x.Volume)
            .ThenByDescending(x => x.MaxKg)
            .Take(takeTop)
            .ToListAsync();

        var rows = top.Select(x =>
        {
            var name = exerciseNames.TryGetValue(x.ExerciseLocalId, out var n) ? n : "(unknown)";
            return new ExerciseStatsRow(x.ExerciseLocalId, name, x.Sets, x.Reps, x.Volume, x.MaxKg);
        }).ToList();

        return (new StatsSummary(sessions, sets, totalReps, totalVolume), rows);
    }
}
