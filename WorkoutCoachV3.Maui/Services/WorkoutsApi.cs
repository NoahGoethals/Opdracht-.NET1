using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

// API client voor workouts (CRUD + listing met query filters).
public class WorkoutsApi : IWorkoutsApi
{
    // JSON options: property names case-insensitive voor compatibiliteit.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Factory levert een geconfigureerde "Api" HttpClient (base url + auth).
    private readonly IHttpClientFactory _factory;

    public WorkoutsApi(IHttpClientFactory factory) => _factory = factory;

    // Authenticated client voor protected endpoints.
    private HttpClient ApiClient => _factory.CreateClient("Api");

    // Haalt workouts op met optionele search en sort query params.
    public async Task<List<WorkoutDto>> GetAllAsync(string? search, string? sort, CancellationToken ct = default)
    {
        var q = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            q.Add("search=" + Uri.EscapeDataString(search.Trim()));

        if (!string.IsNullOrWhiteSpace(sort))
            q.Add("sort=" + Uri.EscapeDataString(sort.Trim()));

        var url = "api/workouts" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

        var resp = await ApiClient.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<WorkoutDto>>(json, JsonOpts) ?? new();
    }

    // Haalt één workout op via id.
    public async Task<WorkoutDto> GetOneAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/workouts/{id}", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<WorkoutDto>(json, JsonOpts)
               ?? throw new InvalidOperationException("API returned empty workout.");
    }

    // Maakt een nieuwe workout aan en verwacht het created object terug.
    public async Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PostAsJsonAsync("api/workouts", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<WorkoutDto>(JsonOpts, ct);
        return created ?? throw new InvalidOperationException("API returned empty workout.");
    }

    // Update bestaande workout (PUT).
    public async Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PutAsJsonAsync($"api/workouts/{id}", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }

    // Verwijdert workout (DELETE).
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.DeleteAsync($"api/workouts/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
