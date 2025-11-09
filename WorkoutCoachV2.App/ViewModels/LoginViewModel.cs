// LoginViewModel: behandelt inloglogica, zet AuthState en opent MainWindow.

using System;
using System.Linq; 
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.App.Services;
using WorkoutCoachV2.Model.Identity;

namespace WorkoutCoachV2.App.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        // DI + appstate.
        private readonly UserManager<AppUser> _userManager;
        private readonly IServiceProvider _provider;
        private readonly AuthState _auth;

        // Koppelt command aan async inloggen.
        public LoginViewModel(UserManager<AppUser> userManager, IServiceProvider provider, AuthState auth)
        {
            _userManager = userManager;
            _provider = provider;
            _auth = auth;

            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsBusy);
        }

        // Invoer: username of e-mail.
        private string _userNameOrEmail = "";
        public string UserNameOrEmail
        {
            get => _userNameOrEmail;
            set { SetProperty(ref _userNameOrEmail, value); (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        // Invoer: wachtwoord.
        private string _password = "";
        public string Password
        {
            get => _password;
            set { SetProperty(ref _password, value); (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        // UI-state: blokkeert dubbelklikken tijdens login.
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { SetProperty(ref _isBusy, value); (LoginCommand as RelayCommand)?.RaiseCanExecuteChanged(); }
        }

        // Command + event om LoginWindow te sluiten bij succes.
        public ICommand LoginCommand { get; }
        public event Action? RequestClose;

        // Inlogstroom: validatie → user opzoeken → pwd check → rollen → MainWindow openen.
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(UserNameOrEmail) || string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Vul gebruikersnaam/e-mail en wachtwoord in.", "Login",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                IsBusy = true;

                var user = await _userManager.FindByNameAsync(UserNameOrEmail)
                           ?? await _userManager.FindByEmailAsync(UserNameOrEmail);

                if (user == null || !await _userManager.CheckPasswordAsync(user, Password))
                {
                    MessageBox.Show("Ongeldige login.", "Login",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var roles = await _userManager.GetRolesAsync(user);
                _auth.User = user;
                _auth.Roles = roles.ToList(); // IList -> List (IReadOnlyList)

                var shell = _provider.GetRequiredService<MainWindow>();
                shell.Show();

                RequestClose?.Invoke();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Login fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
