using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _vm;

        public LoginWindow()
        {
            InitializeComponent();
            _vm = App.HostApp.Services.GetRequiredService<LoginViewModel>();
            DataContext = _vm;
            _vm.RequestClose += () => this.Close();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            _vm.UserNameOrEmail = UserBox.Text;
            _vm.Password = PwdBox.Password;
            if (_vm.LoginCommand.CanExecute(null))
                _vm.LoginCommand.Execute(null);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
