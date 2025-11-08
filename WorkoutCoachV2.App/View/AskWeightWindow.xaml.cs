using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WorkoutCoachV2.App.View
{
    public partial class AskWeightWindow : Window
    {
        public double? WeightKg { get; private set; }

        public AskWeightWindow(double? defaultValue = null)
        {
            InitializeComponent();
            if (defaultValue.HasValue)
                txtWeight.Text = defaultValue.Value.ToString(CultureInfo.CurrentCulture);

            txtWeight.Focus();
            txtWeight.SelectAll();
        }

        private static readonly Regex _allowed = new Regex(@"^[0-9,\.\b]+$");

        private void txtWeight_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_allowed.IsMatch(e.Text);
        }

        private void txtWeight_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var s = (txtWeight.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(s))
            {
                WeightKg = 0;
                DialogResult = true;
                return;
            }

            s = s.Replace(',', '.');

            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0)
            {
                WeightKg = value;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show(this, "Geef een geldig (niet-negatief) gewicht in kg in.", "Ongeldige waarde",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
