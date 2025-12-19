using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalSessionSet : BaseLocalEntity
{
    [Required]
    public Guid SessionLocalId { get; set; }

    [Required]
    public Guid ExerciseLocalId { get; set; }

    public int SetNumber { get; set; } = 1;

    public int Reps { get; set; }
    public double Weight { get; set; }

    public int? Rpe { get; set; }
    public string? Note { get; set; }

    public bool Completed { get; set; }
}
