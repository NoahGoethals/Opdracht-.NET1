using System.Net;
using System.Net.Http.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;


public class AuthApi : IAuthApi
{
    private readonly IHttpClientFactory _factory;

    public AuthApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NoAuthClient => _factory.CreateClient("ApiNoAuth");
    private HttpClient AuthClient => _factory.CreateClient("Api");

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var res = await NoAuthClient.PostAsJsonAsync("api/auth/login", request, ct);

        if (!res.IsSuccessStatusCode)
        {
            var msg = (await res.Content.ReadAsStringAsync(ct))?.Trim();

            if (res.StatusCode == HttpStatusCode.Forbidden)
                throw new Exception(string.IsNullOrWhiteSpace(msg) ? "Gebruiker is geblokkeerd." : msg);

            if (res.StatusCode == HttpStatusCode.Unauthorized)
                throw new Exception(string.IsNullOrWhiteSpace(msg) ? "Ongeldige login." : msg);

            throw new Exception(string.IsNullOrWhiteSpace(msg)
                ? $"Login failed: {(int)res.StatusCode}"
                : msg);
        }

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
        return data ?? throw new Exception("Login response was empty.");
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var res = await NoAuthClient.PostAsJsonAsync("api/auth/register", request, ct);

        if (!res.IsSuccessStatusCode)
        {
            var msg = (await res.Content.ReadAsStringAsync(ct))?.Trim();
            throw new Exception(string.IsNullOrWhiteSpace(msg)
                ? $"Register failed: {(int)res.StatusCode}"
                : msg);
        }

        var data = await res.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
        return data ?? throw new Exception("Register response was empty.");
    }

    public async Task<CurrentUserDto> MeAsync(CancellationToken ct = default)
    {
        var res = await AuthClient.GetAsync("api/auth/me", ct);

        if (res.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Sessie verlopen. Log opnieuw in.");

        if (res.StatusCode == HttpStatusCode.Forbidden)
            throw new Exception("Gebruiker is geblokkeerd.");

        if (!res.IsSuccessStatusCode)
        {
            var msg = (await res.Content.ReadAsStringAsync(ct))?.Trim();
            throw new Exception(string.IsNullOrWhiteSpace(msg)
                ? $"Me failed: {(int)res.StatusCode}"
                : msg);
        }

        var me = await res.Content.ReadFromJsonAsync<CurrentUserDto>(cancellationToken: ct);
        return me ?? throw new Exception("Me response was empty.");
    }
}
