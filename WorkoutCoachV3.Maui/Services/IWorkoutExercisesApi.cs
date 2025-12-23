using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface IWorkoutExercisesApi
{
    Task<List<WorkoutExerciseDto>> GetAllAsync(int workoutId, CancellationToken ct = default);

    Task ReplaceAllAsync(int workoutId, List<UpsertWorkoutExerciseDto> items, CancellationToken ct = default);
}
