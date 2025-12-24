using Microsoft.Maui.Devices;

namespace WorkoutCoachV3.Maui.Services;

public static class ApiConfig
{
    public static string GetBaseUrl()
    {
        // Android emulator -> host PC
        if (DeviceInfo.Platform == DevicePlatform.Android)
            return "https://10.0.2.2:7289/";

        // Windows dev:
        // Gebruik 127.0.0.1 i.p.v. localhost om IPv6/loopback edge-cases te vermijden.
        return "https://127.0.0.1:7289/";
    }
}
