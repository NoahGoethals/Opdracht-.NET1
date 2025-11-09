// Doel: nieuwe gebruiker registreren via AuthService. Toont foutmeldingen uit Identity en sluit bij succes.

using System.Linq;
using System.Windows;
using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.View
{
    public partial class RegisterWindow : Window
    {
        // AuthService-injectie (gebruikt UserManager onder water)
        private readonly AuthService _auth;

        // Constructor: DI binnenhalen en UI initialiseren
        public RegisterWindow(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        // Klik: leest velden, roept RegisterAsync aan, toont feedback en sluit bij succes
        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var res = await _auth.RegisterAsync(
                tbUser.Text.Trim(),
                tbPass.Password,
                tbMail.Text.Trim(),
                tbDisplay.Text.Trim()
            );

            if (!res.Succeeded)
            {
                var msg = string.Join("\n", res.Errors.Select(x => x.Description));
                MessageBox.Show(msg, "Registreren", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Account aangemaakt. Je kan nu inloggen.", "Registreren",
                MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
    }
}
