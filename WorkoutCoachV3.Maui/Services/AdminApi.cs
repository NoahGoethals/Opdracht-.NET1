using System.Net.Http.Json;

namespace WorkoutCoachV3.Maui.Services;

public class AdminApi : IAdminApi
{
    private readonly IHttpClientFactory _factory;

    public AdminApi(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client => _factory.CreateClient("Api");

    public async Task<List<AdminUserDto>> GetUsersAsync()
    {
        var res = await Client.GetAsync("api/admin/users");
        if (!res.IsSuccessStatusCode)
            throw new Exception($"GetUsers failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");

        var users = await res.Content.ReadFromJsonAsync<List<AdminUserDto>>();
        return users ?? new List<AdminUserDto>();
    }

    public async Task ToggleBlockAsync(string userId)
    {
        var res = await Client.PostAsync($"api/admin/users/{userId}/toggle-block", null);
        if (!res.IsSuccessStatusCode)
            throw new Exception($"ToggleBlock failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
    }

    public async Task SetRoleAsync(string userId, string role)
    {
        var res = await Client.PutAsJsonAsync($"api/admin/users/{userId}/role", new { role });
        if (!res.IsSuccessStatusCode)
            throw new Exception($"SetRole failed: {(int)res.StatusCode} {await res.Content.ReadAsStringAsync()}");
    }
}
