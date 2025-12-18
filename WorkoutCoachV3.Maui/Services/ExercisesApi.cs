using System.Net.Http.Json;

namespace WorkoutCoachV3.Maui.Services;

public class ExercisesApi : IExercisesApi
{
    private readonly IHttpClientFactory _factory;

    public ExercisesApi(IHttpClientFactory factory) => _factory = factory;

    public async Task<IReadOnlyList<ExerciseDto>> GetAllAsync(
        string? search = null,
        string? category = null,
        string sort = "name",
        CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");

        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
            query.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category))
            query.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(sort))
            query.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = "api/exercises" + (query.Count > 0 ? "?" + string.Join("&", query) : "");

        var res = await http.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"Exercises GET failed: {(int)res.StatusCode} {msg}");
        }

        var items = await res.Content.ReadFromJsonAsync<List<ExerciseDto>>(cancellationToken: ct);
        return items ?? [];
    }

    public async Task<ExerciseDto> CreateAsync(ExerciseUpsertDto dto, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");

        var res = await http.PostAsJsonAsync("api/exercises", dto, ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"Exercises POST failed: {(int)res.StatusCode} {msg}");
        }

        var created = await res.Content.ReadFromJsonAsync<ExerciseDto>(cancellationToken: ct);
        return created ?? throw new Exception("Create response was empty.");
    }

    public async Task UpdateAsync(int id, ExerciseUpsertDto dto, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");

        var res = await http.PutAsJsonAsync($"api/exercises/{id}", dto, ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"Exercises PUT failed: {(int)res.StatusCode} {msg}");
        }
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");

        var res = await http.DeleteAsync($"api/exercises/{id}", ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"Exercises DELETE failed: {(int)res.StatusCode} {msg}");
        }
    }
}
