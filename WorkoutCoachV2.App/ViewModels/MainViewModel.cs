using Microsoft.Extensions.DependencyInjection;

namespace WorkoutCoachV2.App.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ExercisesViewModel Exercises { get; }
        public WorkoutsViewModel Workouts { get; }

        public MainViewModel(ExercisesViewModel ex, WorkoutsViewModel wo)
        {
            Exercises = ex;
            Workouts = wo;
        }
    }
}
