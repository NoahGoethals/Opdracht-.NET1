// Contract voor Exercises endpoints (lijst, detail, create, update, delete).
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface IExercisesApi
{
    // Haalt oefeningen op met optionele filters (search/category/sort).
    Task<List<ExerciseDto>> GetAllAsync(
        string? search,
        string? category,
        string? sort,
        CancellationToken ct = default);

    // Detail ophalen van één exercise via id.
    Task<ExerciseDto> GetOneAsync(int id, CancellationToken ct = default);

    // Nieuwe exercise aanmaken en het created object terugkrijgen.
    Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default);

    // Bestaande exercise aanpassen (PUT).
    Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default);

    // Exercise verwijderen (DELETE).
    Task DeleteAsync(int id, CancellationToken ct = default);
}
