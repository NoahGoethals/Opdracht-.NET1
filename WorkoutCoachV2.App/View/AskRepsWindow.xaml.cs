using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace WorkoutCoachV2.App.View
{
    public partial class AskRepsWindow : Window
    {
        public int Reps { get; private set; }

        public AskRepsWindow(int defaultReps = 5)
        {
            InitializeComponent();
            tbReps.Text = defaultReps > 0 ? defaultReps.ToString() : "5";
            tbReps.SelectAll();
            tbReps.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(tbReps.Text, out var reps) && reps >= 1 && reps <= 1000)
            {
                Reps = reps;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Geef een geheel getal tussen 1 en 1000.", "Ongeldige waarde",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                tbReps.Focus();
                tbReps.SelectAll();
            }
        }

        private static readonly Regex Digits = new(@"^\d+$");

        private void tbReps_PreviewTextInput(object sender, TextCompositionEventArgs e)
            => e.Handled = !Digits.IsMatch(e.Text);

        private void tbReps_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!Digits.IsMatch(text)) e.CancelCommand();
            }
            else e.CancelCommand();
        }
    }
}
