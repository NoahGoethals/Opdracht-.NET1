using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace WorkoutCoachV3.Maui.Services;

public static class ApiConfig
{
    private const string BaseUrlKey = "ApiBaseUrl";

    private const string AzureDefaultBaseUrl = "https://workoutcoachv2-web-noah1.azurewebsites.net/";

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

    public static string GetDefaultBaseUrl()
    {
        return AzureDefaultBaseUrl;
    }

    public static void SetBaseUrl(string baseUrl)
    {
        var normalized = Normalize(baseUrl);
        if (!IsValidAbsoluteUrl(normalized))
            throw new ArgumentException(
                "Ongeldige Base URL. Voorbeeld: https://jouwapp.azurewebsites.net/",
                nameof(baseUrl));

        Preferences.Set(BaseUrlKey, normalized);
    }

    public static void ResetToDefault()
    {
        Preferences.Remove(BaseUrlKey);
    }

    public static string? GetStoredOverride()
    {
        var saved = Preferences.Get(BaseUrlKey, string.Empty);
        if (string.IsNullOrWhiteSpace(saved)) return null;

        var normalized = Normalize(saved);
        return IsValidAbsoluteUrl(normalized) ? normalized : null;
    }

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

    private static bool IsValidAbsoluteUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var u)
           && (u.Scheme == Uri.UriSchemeHttp || u.Scheme == Uri.UriSchemeHttps);
}
