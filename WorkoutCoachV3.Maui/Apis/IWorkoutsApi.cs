namespace WorkoutCoachV3.Maui.Apis;

public interface IWorkoutsApi
{
    Task<List<WorkoutDto>> GetAllAsync(string? search, string? sort, CancellationToken ct = default);
    Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
