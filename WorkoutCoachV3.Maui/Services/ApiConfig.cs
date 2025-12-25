using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace WorkoutCoachV3.Maui.Services;


public static class ApiConfig
{
    private const string BaseUrlKey = "ApiBaseUrl";

  
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
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            if (DeviceInfo.DeviceType == DeviceType.Virtual)
                return "http://10.0.2.2:5162/";

            return "http://127.0.0.1:5162/";
        }

        return "https://127.0.0.1:7289/";
    }

    public static void SetBaseUrl(string baseUrl)
    {
        var normalized = Normalize(baseUrl);
        if (!IsValidAbsoluteUrl(normalized))
            throw new ArgumentException("Ongeldige Base URL. Voorbeeld: https://jouwapp.azurewebsites.net/", nameof(baseUrl));

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
