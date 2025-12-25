using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Devices;
using WorkoutCoachV3.Maui.Services;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IApiHealthService _health;

    [ObservableProperty] private string baseUrl = "";
    [ObservableProperty] private string defaultBaseUrl = "";
    [ObservableProperty] private string platformInfo = "";
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private string? status;

    public SettingsViewModel(IApiHealthService health)
    {
        _health = health;

        DefaultBaseUrl = ApiConfig.GetDefaultBaseUrl();
        PlatformInfo = BuildPlatformInfo();

        BaseUrl = ApiConfig.GetStoredOverride() ?? DefaultBaseUrl;
    }

    private static string BuildPlatformInfo()
    {
        var platform = DeviceInfo.Platform.ToString();
        var deviceType = DeviceInfo.DeviceType.ToString();
        return $"Platform: {platform} ({deviceType})";
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
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
        ApiConfig.ResetToDefault();
        DefaultBaseUrl = ApiConfig.GetDefaultBaseUrl();
        BaseUrl = DefaultBaseUrl;
        Status = "↩️ Terug naar standaard Base URL.";
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
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
