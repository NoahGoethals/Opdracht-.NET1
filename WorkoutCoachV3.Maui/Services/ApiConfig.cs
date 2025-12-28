// Device/Storage utilities voor MAUI (Preferences).
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace WorkoutCoachV3.Maui.Services;

// Centrale plek voor Base URL opslag + validatie + default.
public static class ApiConfig
{
    // Preferences key voor override base url.
    private const string BaseUrlKey = "ApiBaseUrl";

    // Default Azure URL die gebruikt wordt als er geen override is.
    private const string AzureDefaultBaseUrl = "https://workoutcoachv2-web-noah1.azurewebsites.net/";

    // Geeft actieve base url terug (override indien geldig, anders default).
    public static string GetBaseUrl()
    {
        var saved = Preferences.Get(BaseUrlKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(saved))
        {
            var normalized = Normalize(saved);
            if (IsValidAbsoluteUrl(normalized))
                return normalized;
        }

        return GetDefaultBaseUrl();
    }

    // Default base url (nu Azure).
    public static string GetDefaultBaseUrl()
    {
        return AzureDefaultBaseUrl;
    }

    // Slaat override base url op na normalisatie + validatie.
    public static void SetBaseUrl(string baseUrl)
    {
        var normalized = Normalize(baseUrl);
        if (!IsValidAbsoluteUrl(normalized))
            throw new ArgumentException(
                "Ongeldige Base URL. Voorbeeld: https://jouwapp.azurewebsites.net/",
                nameof(baseUrl));

        Preferences.Set(BaseUrlKey, normalized);
    }

    // Verwijdert override zodat default opnieuw gebruikt wordt.
    public static void ResetToDefault()
    {
        Preferences.Remove(BaseUrlKey);
    }

    // Geeft enkel de override terug (null als niet aanwezig of ongeldig).
    public static string? GetStoredOverride()
    {
        var saved = Preferences.Get(BaseUrlKey, string.Empty);
        if (string.IsNullOrWhiteSpace(saved)) return null;

        var normalized = Normalize(saved);
        return IsValidAbsoluteUrl(normalized) ? normalized : null;
    }

    // Normaliseert input: trim, schema toevoegen, trailing slash forceren.
    private static string Normalize(string url)
    {
        url = (url ?? string.Empty).Trim();
        if (url.Length == 0) return string.Empty;

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        if (!url.EndsWith("/", StringComparison.Ordinal))
            url += "/";

        return url;
    }

    // Valideert dat het een absolute http/https url is.
    private static bool IsValidAbsoluteUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var u)
           && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
