using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalSession : BaseLocalEntity
{
    [Required]
    public string Title { get; set; } = "";

    public DateTime Date { get; set; } = DateTime.Today;

    public string? Description { get; set; }

    public int? WorkoutRemoteId { get; set; }

    public DateTime? StartedUtc { get; set; }
    public DateTime? EndedUtc { get; set; }
    public string? Notes { get; set; }
}
