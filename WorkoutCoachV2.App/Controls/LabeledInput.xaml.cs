// Compacte label+input control voor consistente formulieren.
using System.Windows;
using System.Windows.Controls;

namespace WorkoutCoachV2.App.Controls
{
    // LabeledInput: container met links een label en rechts een willekeurig inputelement.
    public partial class LabeledInput : UserControl
    {
        // Initialiseert de XAML-component.
        public LabeledInput() => InitializeComponent();

        // LABEL (DependencyProperty)

        // Tekst voor het label aan de linkerkant.
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        // DP-registratie voor Label.
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(
                nameof(Label),
                typeof(string),
                typeof(LabeledInput),
                new PropertyMetadata(string.Empty));

        // INPUT (DependencyProperty)

        // De eigenlijke invoercontent (bijv. TextBox, DatePicker, ComboBox...).
        public object? Input
        {
            get => GetValue(InputProperty);
            set => SetValue(InputProperty, value);
        }

        // DP-registratie voor Input.
        public static readonly DependencyProperty InputProperty =
            DependencyProperty.Register(
                nameof(Input),
                typeof(object),
                typeof(LabeledInput),
                new PropertyMetadata(null));
    }
}
