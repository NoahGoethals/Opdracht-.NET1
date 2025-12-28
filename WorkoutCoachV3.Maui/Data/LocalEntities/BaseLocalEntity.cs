// DataAnnotations bevat attributen zoals [Key] om EF Core te vertellen wat de primary key is.
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Basisclass voor alle lokale entities (SQLite) zodat elke tabel dezelfde sync-velden heeft.
public abstract class BaseLocalEntity
{
    // Primary key voor de lokale database (altijd aanwezig).
    [Key]
    public Guid LocalId { get; set; } = Guid.NewGuid();

    // Id van hetzelfde record op de server (null zolang het nog nooit gesynct is).
    public int? RemoteId { get; set; }

    // Soft delete: record blijft lokaal bestaan maar wordt als verwijderd gemarkeerd voor sync.
    public bool IsDeleted { get; set; } = false;

    // Sync status: bv. Dirty/Clean/Conflict (afhankelijk van jouw SyncState enum).
    public SyncState SyncState { get; set; } = SyncState.Dirty;

    // Laatste wijzigingstijd in UTC (handig om conflicts te detecteren).
    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    // Laatste succesvolle sync-tijd in UTC (null als nog nooit gesynct).
    public DateTime? LastSyncedUtc { get; set; }
}
