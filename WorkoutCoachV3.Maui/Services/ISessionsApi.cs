namespace WorkoutCoachV3.Maui.Services;

public record SessionSetDto(int ExerciseId, int SetNumber, int Reps, double Weight);

public record SessionDto(
    int Id,
    string Title,
    DateTime Date,
    string? Description,
    List<SessionSetDto> Sets
);

public record CreateSessionDto(string Title, DateTime Date, string? Description, List<SessionSetDto> Sets);
public record UpdateSessionDto(string Title, DateTime Date, string? Description, List<SessionSetDto> Sets);

public interface ISessionsApi
{
    Task<List<SessionDto>> GetAllAsync(
        string? search,
        DateTime? from,
        DateTime? to,
        string? sort,
        bool includeSets = false,
        CancellationToken ct = default);

    Task<SessionDto> CreateAsync(CreateSessionDto dto, CancellationToken ct = default);
    Task UpdateAsync(int id, UpdateSessionDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
