using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public class WorkoutExercisesApi : IWorkoutExercisesApi
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _factory;

    public WorkoutExercisesApi(IHttpClientFactory factory) => _factory = factory;

    private HttpClient ApiClient => _factory.CreateClient("Api");

    public async Task<List<WorkoutExerciseDto>> GetAllAsync(int workoutId, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/workouts/{workoutId}/exercises", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<WorkoutExerciseDto>>(json, JsonOpts) ?? new();
    }

    public async Task ReplaceAllAsync(int workoutId, List<UpsertWorkoutExerciseDto> items, CancellationToken ct = default)
    {
        items ??= new();

        var resp = await ApiClient.PutAsJsonAsync($"api/workouts/{workoutId}/exercises", items, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }
}
