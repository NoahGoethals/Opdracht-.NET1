// Loginvenster (code-behind):
// - Gebruikt AuthService voor inloggen
// - Op succes: opent MainWindow via DI, sluit loginvenster
// - Registreren opent RegisterWindow als dialoog

using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.View
{
    public partial class LoginWindow : Window
    {
        // Auth-dienst met CurrentUser + Roles
        private readonly AuthService _auth;

        // DI-constructor
        public LoginWindow(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        // Probeer in te loggen met ingevulde velden
        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var ok = await _auth.LoginAsync(tbUser.Text.Trim(), tbPass.Password);
            if (!ok)
            {
                MessageBox.Show("Login mislukt.", "Login", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Succes: toon MainWindow uit DI en sluit dit venster
            var main = App.HostApp.Services.GetRequiredService<MainWindow>();
            main.Show();
            Close();
        }

        // Open registratievenster als dialoog
        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var wnd = App.HostApp.Services.GetRequiredService<RegisterWindow>();
            wnd.Owner = this;
            wnd.ShowDialog();
        }
    }
}
