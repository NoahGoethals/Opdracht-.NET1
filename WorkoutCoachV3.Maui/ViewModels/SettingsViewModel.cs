using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    // Ping service om snel te testen of de API bereikbaar is.
    private readonly IApiHealthService _health;

    // BaseUrl = user override (Preferences) of default, plus UI status.
    [ObservableProperty] private string baseUrl = "";
    [ObservableProperty] private string defaultBaseUrl = "";
    [ObservableProperty] private string platformInfo = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? status;

    public SettingsViewModel(IApiHealthService health)
    {
        _health = health;

        // Default komt uit ApiConfig (Azure URL in je project).
        DefaultBaseUrl = ApiConfig.GetDefaultBaseUrl();
        PlatformInfo = BuildPlatformInfo();

        // Toon override als die bestaat, anders default invullen.
        BaseUrl = ApiConfig.GetStoredOverride() ?? DefaultBaseUrl;
    }

    private static string BuildPlatformInfo()
    {
        // Info voor de gebruiker (handig bij emulator vs phone).
        var platform = DeviceInfo.Platform.ToString();
        var deviceType = DeviceInfo.DeviceType.ToString();
        return $"Platform: {platform} ({deviceType})";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Save: valideert + normaliseert in ApiConfig en bewaart in Preferences.
        if (IsBusy) return;
        IsBusy = true;
        Status = null;

        try
        {
            ApiConfig.SetBaseUrl(BaseUrl);
            Status = "✅ Base URL opgeslagen.";
        }
        catch (Exception ex)
        {
            Status = "❌ " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ResetToDefault()
    {
        // Reset: verwijdert override key en zet UI terug naar default.
        ApiConfig.ResetToDefault();
        DefaultBaseUrl = ApiConfig.GetDefaultBaseUrl();
        BaseUrl = DefaultBaseUrl;
        Status = "↩️ Terug naar standaard Base URL.";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        // Test: eerst BaseUrl opslaan/valideren, dan ping endpoint proberen.
        if (IsBusy) return;
        IsBusy = true;
        Status = "Testing...";

        try
        {
            ApiConfig.SetBaseUrl(BaseUrl);

            var ok = await _health.IsApiReachableAsync();
            Status = ok
                ? "✅ API is bereikbaar (ping)."
                : "❌ API is niet bereikbaar.";
        }
        catch (Exception ex)
        {
            Status = "❌ " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
