using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface IWorkoutsApi
{
    Task<List<WorkoutDto>> GetAllAsync(
        string? search,
        string? sort,
        CancellationToken ct = default);

    Task<WorkoutDto> GetOneAsync(int id, CancellationToken ct = default);

    Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
