namespace WorkoutCoachV3.Maui.Services;

public interface IExercisesApi
{
    Task<IReadOnlyList<ExerciseDto>> GetAllAsync(
        string? search = null,
        string? category = null,
        string sort = "name",
        CancellationToken ct = default);

    Task<ExerciseDto> CreateAsync(ExerciseUpsertDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, ExerciseUpsertDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public record ExerciseDto(int Id, string Name, string? Category, string? Notes);

public record ExerciseUpsertDto(string Name, string? Category, string? Notes);
