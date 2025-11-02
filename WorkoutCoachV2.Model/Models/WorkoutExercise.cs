namespace WorkoutCoachV2.Model.Models;

public class WorkoutExercise : BaseEntity
{
    public int WorkoutId { get; set; }
    public Workout? Workout { get; set; }

    public int ExerciseId { get; set; }
    public Exercise? Exercise { get; set; }

    public int Sets { get; set; }
    public int Reps { get; set; }
    public string? Notes { get; set; }
}
