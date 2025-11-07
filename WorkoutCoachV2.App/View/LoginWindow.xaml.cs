using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.View
{
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth;

        public LoginWindow(AuthService auth)
        {
            InitializeComponent();
            _auth = auth;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var ok = await _auth.LoginAsync(tbUser.Text.Trim(), tbPass.Password);
            if (!ok)
            {
                MessageBox.Show("Login mislukt.", "Login", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var main = App.HostApp.Services.GetRequiredService<MainWindow>();
            main.Show();
            Close();
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var wnd = App.HostApp.Services.GetRequiredService<RegisterWindow>();
            wnd.Owner = this;
            wnd.ShowDialog();
        }
    }
}
