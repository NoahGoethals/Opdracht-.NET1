using System.Net.Http.Headers;

namespace WorkoutCoachV2.Maui.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;

    public AuthHeaderHandler(ITokenStore tokenStore) => _tokenStore = tokenStore;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (token, exp, _) = await _tokenStore.LoadAsync();

        if (!string.IsNullOrWhiteSpace(token) && exp.HasValue && exp.Value > DateTime.UtcNow.AddMinutes(1))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
