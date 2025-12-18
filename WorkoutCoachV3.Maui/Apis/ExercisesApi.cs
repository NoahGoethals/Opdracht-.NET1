using System.Net.Http.Json;

namespace WorkoutCoachV3.Maui.Apis;

public class ExercisesApi : IExercisesApi
{
    private readonly IHttpClientFactory _factory;

    public ExercisesApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<ExerciseDto>> GetAllAsync(string? search, string? category, string? sort, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");

        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category)) qs.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(sort)) qs.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = "api/exercises" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");
        return await http.GetFromJsonAsync<List<ExerciseDto>>(url, ct) ?? new();
    }

    public async Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.PostAsJsonAsync("api/exercises", dto, ct);
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<ExerciseDto>(cancellationToken: ct))!;
    }

    public async Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.PutAsJsonAsync($"api/exercises/{id}", dto, ct);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.DeleteAsync($"api/exercises/{id}", ct);
        res.EnsureSuccessStatusCode();
    }
}
