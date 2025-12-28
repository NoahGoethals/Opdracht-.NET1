using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

// API client voor workout-exercises (lijst ophalen + bulk replace).
public class WorkoutExercisesApi : IWorkoutExercisesApi
{
    // JSON options: property names case-insensitive voor compatibiliteit.
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Factory levert een geconfigureerde "Api" HttpClient (base url + auth).
    private readonly IHttpClientFactory _factory;

    public WorkoutExercisesApi(IHttpClientFactory factory) => _factory = factory;

    // Authenticated client voor protected endpoints.
    private HttpClient ApiClient => _factory.CreateClient("Api");

    // Haalt alle exercise links voor een workout op.
    public async Task<List<WorkoutExerciseDto>> GetAllAsync(int workoutId, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/workouts/{workoutId}/exercises", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<WorkoutExerciseDto>>(json, JsonOpts) ?? new();
    }

    // Vervangt de volledige lijst workout-exercises (server maakt links gelijk aan payload).
    public async Task ReplaceAllAsync(int workoutId, List<UpsertWorkoutExerciseDto> items, CancellationToken ct = default)
    {
        items ??= new();

        var resp = await ApiClient.PutAsJsonAsync($"api/workouts/{workoutId}/exercises", items, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }
}
