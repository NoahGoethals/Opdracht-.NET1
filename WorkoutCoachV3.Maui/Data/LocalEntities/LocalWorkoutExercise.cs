namespace WorkoutCoachV3.Maui.Data.LocalEntities;


public class LocalWorkoutExercise : BaseLocalEntity
{
    public Guid WorkoutLocalId { get; set; }

    public Guid ExerciseLocalId { get; set; }

    public int Reps { get; set; }
    public double WeightKg { get; set; }
}
