using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace WorkoutCoachV3.Maui.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ITokenStore _tokens;
    private readonly IUserSessionStore _session;

    public AuthHeaderHandler(ITokenStore tokens, IUserSessionStore session)
    {
        _tokens = tokens;
        _session = session;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _tokens.GetTokenAsync();

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _tokens.ClearAsync();
                await _session.ClearAsync();
            }

            return response;
        }
        catch (HttpRequestException ex)
        {
        
            Debug.WriteLine($"[API] HttpRequestException: {ex.Message}");

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "API unreachable"
            };
        }
        catch (TaskCanceledException ex)
        {
            Debug.WriteLine($"[API] Timeout/TaskCanceledException: {ex.Message}");

            return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                ReasonPhrase = "API timeout"
            };
        }
    }
}
