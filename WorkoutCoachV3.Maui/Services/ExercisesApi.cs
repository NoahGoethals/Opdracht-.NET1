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
}
