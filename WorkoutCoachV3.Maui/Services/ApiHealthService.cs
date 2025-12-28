namespace WorkoutCoachV3.Maui.Services;

// Kleine service om te testen of de API bereikbaar is (zonder echte login).
public class ApiHealthService : IApiHealthService
{
    // Factory maakt een client met juiste BaseAddress (via DI).
    private readonly IHttpClientFactory _factory;

    public ApiHealthService(IHttpClientFactory factory) => _factory = factory;

    // Ping endpoint: Unauthorized telt ook als "reachable" (server reageert).
    public async Task<bool> IsApiReachableAsync(CancellationToken ct = default)
    {
        try
        {
            var http = _factory.CreateClient(nameof(ApiHealthService));

            using var res = await http.GetAsync("api/ping", ct);

            return res.StatusCode == System.Net.HttpStatusCode.Unauthorized
                   || res.IsSuccessStatusCode;
        }
        catch
        {
            // Netwerk/URL errors → niet bereikbaar.
            return false;
        }
    }
}
