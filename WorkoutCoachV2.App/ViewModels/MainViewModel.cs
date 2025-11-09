// MainViewModel: exposeert Greeting en tab-zichtbaarheid op basis van rollen.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _auth;
        public event PropertyChangedEventHandler? PropertyChanged;

        // AuthService bepaalt welke tabs zichtbaar zijn.
        public MainViewModel(AuthService auth)
        {
            _auth = auth;
        }

        // Header-groet (fallback "Admin" als CurrentUser null is).
        public string Greeting => _auth.CurrentUser is null ? "Admin" : _auth.CurrentUser.DisplayName;

        // Toegangscontrole per tab.
        public bool CanSeeExercises => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeWorkouts => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeSessions => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeStats => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeUserAdmin => _auth.IsInRole("Admin");

        // Notificatiehelper.
        protected void Raise([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
