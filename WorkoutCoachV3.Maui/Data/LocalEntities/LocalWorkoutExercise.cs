namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalWorkoutExercise : BaseLocalEntity
{
    public Guid WorkoutLocalId { get; set; }
    public Guid ExerciseLocalId { get; set; }

    public int Repetitions { get; set; }
    public double WeightKg { get; set; }

    public LocalWorkout? Workout { get; set; }
    public LocalExercise? Exercise { get; set; }
}
