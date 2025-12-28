// Importeert CultureInfo zodat de converter cultuurinfo kan ontvangen (vereist door IValueConverter).
using System.Globalization;

// Namespace voor alle value converters in de MAUI-app.
namespace WorkoutCoachV3.Maui.Converters;

// Converter die controleert of een string niet null/empty/whitespace is (handig voor IsEnabled/IsVisible bindings).
public sealed class StringNotNullOrEmptyConverter : IValueConverter
{
    // Wordt gebruikt bij binding van ViewModel -> UI: zet de waarde om naar een boolean.
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Cast de waarde naar string (geeft null als value geen string is).
        var s = value as string;
        // Geeft true terug als de string niet null/empty is en niet enkel uit spaties bestaat.
        return !string.IsNullOrWhiteSpace(s);
    }

    // ConvertBack is niet voorzien/ondersteund voor deze converter.
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        // Gooit een exception omdat terugconverteren hier geen zin heeft in databinding.
        => throw new NotSupportedException();
}
