using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

// :contentReference[oaicite:2]{index=2} // Exercises API client: CRUD + query params (search/category/sort).
namespace WorkoutCoachV3.Maui.Services;

public class ExercisesApi : IExercisesApi
{
    // JSON options: case-insensitive property mapping (handig bij API casing verschillen).
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Factory levert "Api" client (met auth handler + base url).
    private readonly IHttpClientFactory _factory;

    public ExercisesApi(IHttpClientFactory factory) => _factory = factory;

    // Authenticated client voor alle exercise endpoints.
    private HttpClient ApiClient => _factory.CreateClient("Api");

    // Haalt lijst op met optionele filters (search/category/sort) via query string.
    public async Task<List<ExerciseDto>> GetAllAsync(
        string? search,
        string? category,
        string? sort,
        CancellationToken ct = default)
    {
        // Bouw query parameters dynamisch op.
        var q = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            q.Add("search=" + Uri.EscapeDataString(search.Trim()));

        if (!string.IsNullOrWhiteSpace(category))
            q.Add("category=" + Uri.EscapeDataString(category.Trim()));

        if (!string.IsNullOrWhiteSpace(sort))
            q.Add("sort=" + Uri.EscapeDataString(sort.Trim()));

        // Endpoint: api/exercises?...
        var url = "api/exercises" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

        var resp = await ApiClient.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        // Leest raw json om daarna expliciet te deserializen met JsonOpts.
        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<ExerciseDto>>(json, JsonOpts) ?? new();
    }

    // Haalt één exercise op via id.
    public async Task<ExerciseDto> GetOneAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/exercises/{id}", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<ExerciseDto>(json, JsonOpts)
               ?? throw new InvalidOperationException("API returned empty exercise.");
    }

    // Maakt een nieuwe exercise aan en verwacht het created object terug.
    public async Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PostAsJsonAsync("api/exercises", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<ExerciseDto>(JsonOpts, ct);
        return created ?? throw new InvalidOperationException("API returned empty exercise.");
    }

    // Update bestaande exercise (PUT).
    public async Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PutAsJsonAsync($"api/exercises/{id}", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }

    // Delete bestaande exercise (DELETE).
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.DeleteAsync($"api/exercises/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
