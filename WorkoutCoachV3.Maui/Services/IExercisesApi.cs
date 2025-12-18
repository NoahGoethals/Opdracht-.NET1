namespace WorkoutCoachV3.Maui.Services;

public interface IExercisesApi
{
    Task<IReadOnlyList<ExerciseDto>> GetAllAsync(
        string? search = null,
        string? category = null,
        string sort = "name",
        CancellationToken ct = default);
}

public record ExerciseDto(int Id, string Name, string? Category, string? Notes);
