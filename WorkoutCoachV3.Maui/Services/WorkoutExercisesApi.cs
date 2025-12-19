using System.Net.Http.Json;

namespace WorkoutCoachV3.Maui.Services;

public class WorkoutExercisesApi : IWorkoutExercisesApi
{
    private readonly IHttpClientFactory _factory;

    public WorkoutExercisesApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<List<WorkoutExerciseLinkDto>> GetAllAsync(int workoutId, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        return await http.GetFromJsonAsync<List<WorkoutExerciseLinkDto>>(
                   $"api/workouts/{workoutId}/exercises", ct
               ) ?? new();
    }

    public async Task ReplaceAllAsync(int workoutId, List<UpsertWorkoutExerciseLinkDto> items, CancellationToken ct = default)
    {
        var http = _factory.CreateClient("Api");
        var res = await http.PutAsJsonAsync($"api/workouts/{workoutId}/exercises", items, ct);
        res.EnsureSuccessStatusCode();
    }
}
