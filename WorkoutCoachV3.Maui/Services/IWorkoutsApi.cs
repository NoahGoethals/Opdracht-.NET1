namespace WorkoutCoachV3.Maui.Services;

public record WorkoutDto(int Id, string Title, DateTime? ScheduledOn);
public record CreateWorkoutDto(string Title, DateTime? ScheduledOn);
public record UpdateWorkoutDto(string Title, DateTime? ScheduledOn);

public interface IWorkoutsApi
{
    Task<List<WorkoutDto>> GetAllAsync(string? search, string? sort, CancellationToken ct = default);
    Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
