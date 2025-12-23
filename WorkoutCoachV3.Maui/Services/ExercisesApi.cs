using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public class ExercisesApi : IExercisesApi
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _factory;

    public ExercisesApi(IHttpClientFactory factory) => _factory = factory;

    private HttpClient ApiClient => _factory.CreateClient("Api");

    public async Task<List<ExerciseDto>> GetAllAsync(
        string? search,
        string? category,
        string? sort,
        CancellationToken ct = default)
    {
        var q = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            q.Add("search=" + Uri.EscapeDataString(search.Trim()));

        if (!string.IsNullOrWhiteSpace(category))
            q.Add("category=" + Uri.EscapeDataString(category.Trim()));

        if (!string.IsNullOrWhiteSpace(sort))
            q.Add("sort=" + Uri.EscapeDataString(sort.Trim()));

        var url = "api/exercises" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

        var resp = await ApiClient.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<ExerciseDto>>(json, JsonOpts) ?? new();
    }

    public async Task<ExerciseDto> GetOneAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/exercises/{id}", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<ExerciseDto>(json, JsonOpts)
               ?? throw new InvalidOperationException("API returned empty exercise.");
    }

    public async Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PostAsJsonAsync("api/exercises", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<ExerciseDto>(JsonOpts, ct);
        return created ?? throw new InvalidOperationException("API returned empty exercise.");
    }

    public async Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PutAsJsonAsync($"api/exercises/{id}", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.DeleteAsync($"api/exercises/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
