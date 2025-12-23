using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public class WorkoutsApi : IWorkoutsApi
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _factory;

    public WorkoutsApi(IHttpClientFactory factory) => _factory = factory;

    private HttpClient ApiClient => _factory.CreateClient("Api");

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

    public async Task<WorkoutDto> GetOneAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/workouts/{id}", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<WorkoutDto>(json, JsonOpts)
               ?? throw new InvalidOperationException("API returned empty workout.");
    }

    public async Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PostAsJsonAsync("api/workouts", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<WorkoutDto>(JsonOpts, ct);
        return created ?? throw new InvalidOperationException("API returned empty workout.");
    }

    public async Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PutAsJsonAsync($"api/workouts/{id}", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.DeleteAsync($"api/workouts/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
