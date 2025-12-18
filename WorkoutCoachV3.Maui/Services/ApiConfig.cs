using Microsoft.Maui.Devices;

namespace WorkoutCoachV3.Maui.Services;

public static class ApiConfig
{
    public static string GetBaseUrl()
    {
        // Android emulator -> host PC
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "https://10.0.2.2:7289/";

        // Windows dev -> exact hetzelfde als je browser test
        return "https://localhost:7289/";
    }
}
