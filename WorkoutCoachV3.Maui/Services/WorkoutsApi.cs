using System.Net.Http.Json;
using System.Text.Json;

namespace WorkoutCoachV3.Maui.Services;

public class WorkoutsApi : IWorkoutsApi
{
    private readonly IHttpClientFactory _factory;

    public WorkoutsApi(IHttpClientFactory factory) => _factory = factory;

    private HttpClient ApiClient => _factory.CreateClient("Api");

    public async Task<List<WorkoutDto>> GetAllAsync(string? search, string? sort, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(sort)) qs.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = "api/workouts" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        var res = await ApiClient.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Workouts GET failed: {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<WorkoutDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new List<WorkoutDto>();
    }

    public async Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default)
    {
        var res = await ApiClient.PostAsJsonAsync("api/workouts", dto, ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Workouts POST failed: {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<WorkoutDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new Exception("Invalid create response.");
    }

    public async Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default)
    {
        var res = await ApiClient.PutAsJsonAsync($"api/workouts/{id}", dto, ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Workouts PUT failed: {(int)res.StatusCode}");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var res = await ApiClient.DeleteAsync($"api/workouts/{id}", ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Workouts DELETE failed: {(int)res.StatusCode}");
    }
}
