using System.Linq;
using System.Windows;
using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.View
{
    public partial class RegisterWindow : Window
    {
        private readonly AuthService _auth;
        public RegisterWindow(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

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
