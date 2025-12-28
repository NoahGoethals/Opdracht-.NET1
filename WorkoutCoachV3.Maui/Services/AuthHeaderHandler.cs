using System.Net;
using System.Net.Http.Headers;
using System.Diagnostics;

// :contentReference[oaicite:0]{index=0} // DelegatingHandler: voegt Bearer token toe + vangt API fouten af.
namespace WorkoutCoachV3.Maui.Services;

public class AuthHeaderHandler : DelegatingHandler
{
    // Token storage + session storage om auth state te beheren.
    private readonly ITokenStore _tokens;
    private readonly IUserSessionStore _session;

    public AuthHeaderHandler(ITokenStore tokens, IUserSessionStore session)
    {
        _tokens = tokens;
        _session = session;
    }

    // Intercept elke request: voeg Authorization header toe + behandel Unauthorized/timeout/unreachable.
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Haal opgeslagen JWT/AccessToken op (kan null/empty zijn).
        var token = await _tokens.GetTokenAsync();

        // Voeg Bearer token toe wanneer aanwezig.
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            // Stuur request door naar volgende handler / echte HttpClient pipeline.
            var response = await base.SendAsync(request, cancellationToken);

            // Bij 401: token + sessie leegmaken zodat app opnieuw laat inloggen.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _tokens.ClearAsync();
                await _session.ClearAsync();
            }

            return response;
        }
        catch (HttpRequestException ex)
        {

            // Netwerk/DNS/connection failure: log en geef synthetic response terug.
            Debug.WriteLine($"[API] HttpRequestException: {ex.Message}");

            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                ReasonPhrase = "API unreachable"
            };
        }
        catch (TaskCanceledException ex)
        {
            // Timeout of geannuleerde request: log en geef synthetic response terug.
            Debug.WriteLine($"[API] Timeout/TaskCanceledException: {ex.Message}");

            return new HttpResponseMessage(HttpStatusCode.RequestTimeout)
            {
                ReasonPhrase = "API timeout"
            };
        }
    }
}
