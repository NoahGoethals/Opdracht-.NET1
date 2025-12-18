using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalWorkoutExercise
{
    [Key]
    public Guid LocalId { get; set; } = Guid.NewGuid();

    public Guid WorkoutLocalId { get; set; }
    public Guid ExerciseLocalId { get; set; }

    public int Repetitions { get; set; }
    public double WeightKg { get; set; }
}
