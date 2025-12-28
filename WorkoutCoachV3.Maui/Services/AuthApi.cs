// Statuscodes gebruiken voor duidelijke error handling.
using System.Net;
// JSON helpers voor PostAsJsonAsync / ReadFromJsonAsync.
using System.Net.Http.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;


// Auth endpoints wrapper (login/register/me).
public class AuthApi : IAuthApi
{
    // Factory levert clients met/zonder auth header.
    private readonly IHttpClientFactory _factory;

    public AuthApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    // Client zonder auth (login/register).
    private HttpClient NoAuthClient => _factory.CreateClient("ApiNoAuth");
    // Client met auth (me).
    private HttpClient AuthClient => _factory.CreateClient("Api");

    // Login: geeft tokens/user info terug of gooit exception met juiste boodschap.
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

    // Register: maakt account aan en verwacht AuthResponse terug.
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

    // Me: haalt huidige user op met token; behandelt verlopen sessie/blocked expliciet.
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
