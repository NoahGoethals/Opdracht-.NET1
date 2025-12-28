// Contract voor workout-exercises endpoints (lijst + replace all).
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface IWorkoutExercisesApi
{
    // Haalt alle oefeningen van een workout op (server-side).
    Task<List<WorkoutExerciseDto>> GetAllAsync(int workoutId, CancellationToken ct = default);

    // Vervangt volledige lijst (bulk update) voor een workout.
    Task ReplaceAllAsync(int workoutId, List<UpsertWorkoutExerciseDto> items, CancellationToken ct = default);
}
