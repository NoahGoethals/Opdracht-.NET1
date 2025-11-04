using WorkoutCoachV2.App.Services;

namespace WorkoutCoachV2.App.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly AuthState _auth;

        public ExercisesViewModel Exercises { get; }
        public WorkoutsViewModel Workouts { get; }
        public SessionsViewModel Sessions { get; }

        public MainViewModel(ExercisesViewModel ex, WorkoutsViewModel wo, SessionsViewModel se, AuthState auth)
        {
            Exercises = ex;
            Workouts = wo;
            Sessions = se;
            _auth = auth;
        }

        public bool CanSeeExercises => true; 
        public bool CanSeeWorkouts => _auth.IsCoach || _auth.IsAdmin;
        public bool CanSeeSessions => _auth.IsMember || _auth.IsCoach || _auth.IsAdmin;

        public string Greeting => _auth.User?.DisplayName ?? _auth.User?.UserName ?? "User";
    }
}
