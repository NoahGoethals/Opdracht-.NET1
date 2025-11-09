// Popup om gewicht in kg op te vragen (punt of komma toegestaan). 
// - Filtert toetsinvoer op cijfers en .,/
// - Bij OK: parse naar double (InvariantCulture) en valideer op >= 0.

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WorkoutCoachV2.App.View
{
    public partial class AskWeightWindow : Window
    {
        // Resultaat (kg). Null als geannuleerd; 0 indien leeg gelaten en bevestigd.
        public double? WeightKg { get; private set; }

        // Init: optionele defaultwaarde tonen en focus/selectie op invoerveld zetten.
        public AskWeightWindow(double? defaultValue = null)
        {
            InitializeComponent();
            if (defaultValue.HasValue)
                txtWeight.Text = defaultValue.Value.ToString(CultureInfo.CurrentCulture);

            txtWeight.Focus();
            txtWeight.SelectAll();
        }

        // Toegestane karakters bij typen (cijfers, komma, punt). Backspace/Del worden door WPF apart afgehandeld.
        private static readonly Regex _allowed = new Regex(@"^[0-9,\.\b]+$");

        // Blokkeer ongeldige tekens tijdens typen.
        private void txtWeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_allowed.IsMatch(e.Text);
        }

        // Hook aanwezig voor live-validatie (nu niet gebruikt; laat staan voor eenvoud/consistentie).
        private void txtWeight_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }

        // OK: parse naar double (komma => punt), valideer op niet-negatief, sluit met DialogResult = true.
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = (txtWeight.Text ?? string.Empty).Trim();

            // Leeg veld interpreteren als 0 kg.
            if (string.IsNullOrWhiteSpace(s))
            {
                WeightKg = 0;
                DialogResult = true;
                return;
            }

            // Normaliseer komma naar punt voor InvariantCulture.
            s = s.Replace(',', '.');

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0)
            {
                WeightKg = value;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(this,
                    "Geef een geldig (niet-negatief) gewicht in kg in.",
                    "Ongeldige waarde",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
