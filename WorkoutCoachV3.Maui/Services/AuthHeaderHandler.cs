using System.Net;
using System.Net.Http.Headers;

namespace WorkoutCoachV3.Maui.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStore _tokens;

    public AuthHeaderHandler(ITokenStore tokens) => _tokens = tokens;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        
        var token = await _tokens.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _tokens.ClearAsync();
        }

        return response;
    }
}
