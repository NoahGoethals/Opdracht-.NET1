namespace WorkoutCoachV3.Maui.Services;

public record ExerciseDto(int Id, string Name, string? Category, string? Notes);
public record CreateExerciseDto(string Name, string? Category, string? Notes);
public record UpdateExerciseDto(string Name, string? Category, string? Notes);

public interface IExercisesApi
{
    Task<List<ExerciseDto>> GetAllAsync(string? search, string? category, string? sort, CancellationToken ct = default);
    Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
