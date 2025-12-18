namespace WorkoutCoachV3.Maui.Services;

public class ApiHealthService : IApiHealthService
{
    private readonly IHttpClientFactory _factory;

    public ApiHealthService(IHttpClientFactory factory) => _factory = factory;

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
            return false;
        }
    }
}
