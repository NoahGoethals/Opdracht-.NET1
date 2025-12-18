using System.Net.Http.Json;

namespace WorkoutCoachV3.Maui.Apis;

public class WorkoutsApi : IWorkoutsApi
{
    private readonly IHttpClientFactory _factory;

    public WorkoutsApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<WorkoutDto>> GetAllAsync(string? search, string? sort, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");

        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(sort)) qs.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = "api/workouts" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await http.GetFromJsonAsync<List<WorkoutDto>>(url, ct) ?? new();
    }

    public async Task<WorkoutDto> CreateAsync(CreateWorkoutDto dto, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.PostAsJsonAsync("api/workouts", dto, ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<WorkoutDto>(cancellationToken: ct))!;
    }

    public async Task UpdateAsync(int id, UpdateWorkoutDto dto, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.PutAsJsonAsync($"api/workouts/{id}", dto, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.DeleteAsync($"api/workouts/{id}", ct);
        res.EnsureSuccessStatusCode();
    }
}
