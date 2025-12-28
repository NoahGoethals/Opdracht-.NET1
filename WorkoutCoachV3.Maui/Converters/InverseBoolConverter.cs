// Importeert CultureInfo zodat de converter cultuurinfo kan ontvangen (vereist door IValueConverter).
using System.Globalization;

// Namespace voor alle value converters in de MAUI-app.
namespace WorkoutCoachV3.Maui.Converters;

// Converter die booleans omdraait (true -> false, false -> true) voor databinding.
public class InverseBoolConverter : IValueConverter
{
    // Wordt gebruikt bij binding van ViewModel -> UI: zet de waarde om naar het doeltype.
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Controleert of de inkomende waarde een bool is; zo ja, keer hem om.
        if (value is bool b) return !b;
        // Fallback: als value geen bool is (null of ander type), geef true terug.
        return true;
    }

    // Wordt gebruikt bij binding van UI -> ViewModel: zet de waarde terug om.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Controleert of de inkomende waarde een bool is; zo ja, keer hem om.
        if (value is bool b) return !b;
        // Fallback: als value geen bool is (null of ander type), geef false terug.
        return false;
    }
}
