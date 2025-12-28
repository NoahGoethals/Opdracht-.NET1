// Contract voor Workouts API (CRUD + listing met filters).
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface IWorkoutsApi
{
    // Lijst ophalen met optionele search en sort.
    Task<List<WorkoutDto>> GetAllAsync(
        string? search,
        string? sort,
        CancellationToken ct = default);

    // Detail ophalen via id.
    Task<WorkoutDto> GetOneAsync(int id, CancellationToken ct = default);

    // Nieuwe workout aanmaken.
    Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default);

    // Bestaande workout aanpassen.
    Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default);

    // Workout verwijderen.
    Task DeleteAsync(int id, CancellationToken ct = default);
}
