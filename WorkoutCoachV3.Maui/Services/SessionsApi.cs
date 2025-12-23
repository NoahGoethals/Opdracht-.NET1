using System.Net.Http.Json;
using System.Text.Json;
using WorkoutCoachV2.Model.ApiContracts;

namespace WorkoutCoachV3.Maui.Services;

public class SessionsApi : ISessionsApi
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _factory;

    public SessionsApi(IHttpClientFactory factory) => _factory = factory;

    private HttpClient ApiClient => _factory.CreateClient("Api");

    public async Task<List<SessionDto>> GetAllAsync(
        string? search,
        DateTime? from,
        DateTime? to,
        string? sort,
        bool includeSets = false,
        CancellationToken ct = default)
    {
        var q = new List<string>();

        if (!string.IsNullOrWhiteSpace(search))
            q.Add("search=" + Uri.EscapeDataString(search.Trim()));

        if (from.HasValue)
            q.Add("from=" + Uri.EscapeDataString(from.Value.ToString("yyyy-MM-dd")));

        if (to.HasValue)
            q.Add("to=" + Uri.EscapeDataString(to.Value.ToString("yyyy-MM-dd")));

        if (!string.IsNullOrWhiteSpace(sort))
            q.Add("sort=" + Uri.EscapeDataString(sort.Trim()));

        if (includeSets)
            q.Add("includeSets=true");

        var url = "api/sessions" + (q.Count > 0 ? "?" + string.Join("&", q) : "");

        var resp = await ApiClient.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<List<SessionDto>>(json, JsonOpts) ?? new();
    }

    public async Task<SessionDto> GetOneAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.GetAsync($"api/sessions/{id}", ct);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<SessionDto>(json, JsonOpts)
               ?? throw new InvalidOperationException("API returned empty session.");
    }

    public async Task<SessionDto> CreateAsync(UpsertSessionDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PostAsJsonAsync("api/sessions", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();

        var created = await resp.Content.ReadFromJsonAsync<SessionDto>(JsonOpts, ct);
        return created ?? throw new InvalidOperationException("API returned empty session.");
    }

    public async Task UpdateAsync(int id, UpsertSessionDto dto, CancellationToken ct = default)
    {
        var resp = await ApiClient.PutAsJsonAsync($"api/sessions/{id}", dto, JsonOpts, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var resp = await ApiClient.DeleteAsync($"api/sessions/{id}", ct);
        resp.EnsureSuccessStatusCode();
    }
}
