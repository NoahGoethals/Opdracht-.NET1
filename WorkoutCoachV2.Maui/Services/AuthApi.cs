using System.Net.Http.Json;

namespace WorkoutCoachV2.Maui.Services;

public class AuthApi : IAuthApi
{
    private readonly HttpClient _http;

    public AuthApi(HttpClient http) => _http = http;

    public async Task<AuthResponse> LoginAsync(string email, string password)
    {
        var body = new { Email = email, Password = password };

        var resp = await _http.PostAsJsonAsync("api/auth/login", body);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = await resp.Content.ReadAsStringAsync();
            throw new Exception($"Login failed: {(int)resp.StatusCode} - {msg}");
        }

        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>()
                   ?? throw new Exception("Lege response van server.");

        return data;
    }
}
