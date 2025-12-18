using System.Net.Http.Json;
using System.Text.Json;

namespace WorkoutCoachV3.Maui.Services;

public class ExercisesApi : IExercisesApi
{
    private readonly IHttpClientFactory _factory;

    public ExercisesApi(IHttpClientFactory factory) => _factory = factory;

    private HttpClient ApiClient => _factory.CreateClient("Api");

    public async Task<List<ExerciseDto>> GetAllAsync(string? search, string? category, string? sort, CancellationToken ct = default)
    {
        var qs = new List<string>();
        if (!string.IsNullOrWhiteSpace(search)) qs.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(category)) qs.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrWhiteSpace(sort)) qs.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = "api/exercises" + (qs.Count > 0 ? "?" + string.Join("&", qs) : "");

        var res = await ApiClient.GetAsync(url, ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Exercises GET failed: {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<ExerciseDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? new List<ExerciseDto>();
    }

    public async Task<ExerciseDto> CreateAsync(CreateExerciseDto dto, CancellationToken ct = default)
    {
        var res = await ApiClient.PostAsJsonAsync("api/exercises", dto, ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Exercises POST failed: {(int)res.StatusCode}");

        var json = await res.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<ExerciseDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new Exception("Invalid create response.");
    }

    public async Task UpdateAsync(int id, UpdateExerciseDto dto, CancellationToken ct = default)
    {
        var res = await ApiClient.PutAsJsonAsync($"api/exercises/{id}", dto, ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Exercises PUT failed: {(int)res.StatusCode}");
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var res = await ApiClient.DeleteAsync($"api/exercises/{id}", ct);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"Exercises DELETE failed: {(int)res.StatusCode}");
    }
}
