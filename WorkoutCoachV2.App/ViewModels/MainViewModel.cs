using System.ComponentModel;
using System.Runtime.CompilerServices;
using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _auth;
        public event PropertyChangedEventHandler? PropertyChanged;

        public MainViewModel(AuthService auth)
        {
            _auth = auth;
        }

        public string Greeting => _auth.CurrentUser is null ? "Admin" : _auth.CurrentUser.DisplayName;

        public bool CanSeeExercises => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeWorkouts => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeSessions => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeStats => _auth.IsInRole("Admin") || _auth.IsInRole("User");
        public bool CanSeeUserAdmin => _auth.IsInRole("Admin");

        protected void Raise([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }
}
