using Microsoft.Maui.Devices;

namespace WorkoutCoachV3.Maui.Services;

public static class ApiConfig
{
    public static string GetBaseUrl()
    {
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // ✅ Voor echte telefoon + USB (adb reverse) EN emulator:
            // - adb reverse maakt telefoon localhost:5162 -> PC localhost:5162
            // - emulator kan ook naar zichzelf, maar 127.0.0.1 werkt enkel als reverse actief is
            // Dus: we kiezen 127.0.0.1 en gebruiken adb reverse als standaard testpad.
            return "http://127.0.0.1:5162/";
        }

        // ✅ Windows dev
        return "https://127.0.0.1:7289/";
    }
}
