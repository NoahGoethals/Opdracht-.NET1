// JSON helpers voor HttpClient (ReadFromJsonAsync / PutAsJsonAsync).
using System.Net.Http.Json;

namespace WorkoutCoachV3.Maui.Services;

// Admin endpoints wrapper (users laden, block togglen, role zetten).
public class AdminApi : IAdminApi
{
    // Factory levert vooraf geconfigureerde clients (base url, auth handler, etc.).
    private readonly IHttpClientFactory _factory;

    public AdminApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    // Authenticated client (naam "Api" wordt in DI geconfigureerd).
    private HttpClient Client => _factory.CreateClient("Api");

    // Haalt admin user lijst op via API.
    public async Task<List<AdminUserDto>> GetUsersAsync()
    {
        var res = await Client.GetAsync("api/admin/users");
        if (!res.IsSuccessStatusCode)
            throw new Exception($"GetUsers failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");

        var users = await res.Content.ReadFromJsonAsync<List<AdminUserDto>>();
        return users ?? new List<AdminUserDto>();
    }

    // Blokkeer/deblokkeer een user (server beslist nieuwe status).
    public async Task ToggleBlockAsync(string userId)
    {
        var res = await Client.PostAsync($"api/admin/users/{userId}/toggle-block", null);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"ToggleBlock failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
    }

    // Zet de rol van een user via PUT payload { role }.
    public async Task SetRoleAsync(string userId, string role)
    {
        var res = await Client.PutAsJsonAsync($"api/admin/users/{userId}/role", new { role });
        if (!res.IsSuccessStatusCode)
            throw new Exception($"SetRole failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
    }
}
