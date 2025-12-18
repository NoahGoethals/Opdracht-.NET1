namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalSession : BaseLocalEntity
{
    public int? WorkoutRemoteId { get; set; } 
    public DateTime StartedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedUtc { get; set; }
    public string? Notes { get; set; }
}
