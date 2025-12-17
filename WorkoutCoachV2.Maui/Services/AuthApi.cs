using System.Net.Http.Json;
using WorkoutCoachV2.Maui.Models;

namespace WorkoutCoachV2.Maui.Services;

public interface IAuthApi
{
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default);
}

public class AuthApi : IAuthApi
{
    private readonly HttpClient _http;

    public AuthApi(HttpClient http) => _http = http;

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/Auth/login", new LoginRequest(email, password), ct);
        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync(ct);
            throw new Exception($"Login failed: {(int)res.StatusCode} {msg}");
        }

        var body = await res.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: ct);
        return body ?? throw new Exception("Login failed: empty response");
    }
}
