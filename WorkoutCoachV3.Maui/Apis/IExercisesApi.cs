namespace WorkoutCoachV3.Maui.Apis;

public interface IExercisesApi
{
    Task<List<ExerciseDto>> GetAllAsync(string? search, string? category, string? sort, CancellationToken ct = default);
    Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
