using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public abstract class BaseLocalEntity
{
    [Key]
    public Guid LocalId { get; set; } = Guid.NewGuid();

    public int? RemoteId { get; set; }

    public bool IsDeleted { get; set; } = false;

    public SyncState SyncState { get; set; } = SyncState.Dirty;

    public DateTime LastModifiedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncedUtc { get; set; }
}
