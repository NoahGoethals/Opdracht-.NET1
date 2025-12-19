namespace WorkoutCoachV3.Maui.Services;

public sealed class WorkoutExerciseLinkDto
{
    public int ExerciseId { get; set; }
    public string ExerciseName { get; set; } = "";
    public int Reps { get; set; }
    public double? WeightKg { get; set; }
}

public sealed class UpsertWorkoutExerciseLinkDto
{
    public int ExerciseId { get; set; }
    public int Reps { get; set; }
    public double? WeightKg { get; set; }
}

public interface IWorkoutExercisesApi
{
    Task<List<WorkoutExerciseLinkDto>> GetAllAsync(int workoutId, CancellationToken ct = default);
    Task ReplaceAllAsync(int workoutId, List<UpsertWorkoutExerciseLinkDto> items, CancellationToken ct = default);
}
