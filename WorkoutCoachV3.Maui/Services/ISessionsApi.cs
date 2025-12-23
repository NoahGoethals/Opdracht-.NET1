using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface ISessionsApi
{
    Task<List<SessionDto>> GetAllAsync(
        string? search,
        DateTime? from,
        DateTime? to,
        string? sort,
        bool includeSets = false,
        CancellationToken ct = default);

    Task<SessionDto> GetOneAsync(int id, CancellationToken ct = default);

    Task<SessionDto> CreateAsync(UpsertSessionDto dto, CancellationToken ct = default);

    Task UpdateAsync(int id, UpsertSessionDto dto, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);
}
