using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface IExercisesApi
{
    Task<List<ExerciseDto>> GetAllAsync(
        string? search,
        string? category,
        string? sort,
        CancellationToken ct = default);

    Task<ExerciseDto> GetOneAsync(int id, CancellationToken ct = default);

    Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
