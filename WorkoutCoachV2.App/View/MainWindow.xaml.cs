using System.Threading.Tasks;
using System.Windows;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;

            Loaded += async (_, __) => await LoadAsync(vm);
        }

        private static async Task LoadAsync(MainViewModel vm)
        {
            await vm.Exercises.LoadAsync();
            await vm.Workouts.LoadAsync();
        }
    }
}
