// Contract voor Sessions endpoints (lijst/detail + create/update/delete + optioneel sets includen).
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public interface ISessionsApi
{
    // Haalt sessies op met filters (search/date range/sort) en optioneel sets.
    Task<List<SessionDto>> GetAllAsync(
        string? search,
        DateTime? from,
        DateTime? to,
        string? sort,
        bool includeSets = false,
        CancellationToken ct = default);

    // Detail ophalen van één sessie via id.
    Task<SessionDto> GetOneAsync(int id, CancellationToken ct = default);

    // Nieuwe sessie aanmaken (met upsert DTO).
    Task<SessionDto> CreateAsync(UpsertSessionDto dto, CancellationToken ct = default);

    // Bestaande sessie aanpassen (PUT).
    Task UpdateAsync(int id, UpsertSessionDto dto, CancellationToken ct = default);

    // Sessie verwijderen (DELETE).
    Task DeleteAsync(int id, CancellationToken ct = default);
}
