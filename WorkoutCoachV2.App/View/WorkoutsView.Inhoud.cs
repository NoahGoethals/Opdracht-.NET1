using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class WorkoutsView : UserControl
    {
        private async void btnInhoud_Click(object sender, RoutedEventArgs e)
        {
            if (dgWorkouts?.SelectedItem is not Workout selected)
            {
                MessageBox.Show("Selecteer eerst een workout.", "Inhoud beheren",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new EditWorkoutExercisesWindow(selected.Id)
            {
                Owner = Window.GetWindow(this)
            };

            var ok = dlg.ShowDialog() == true;
            if (ok) await TryRefreshAsync();
        }

        private async Task TryRefreshAsync()
        {
            var vm = DataContext;
            if (vm == null) return;

            var mi = vm.GetType().GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                var result = mi.Invoke(vm, null);
                if (result is Task t) await t;
                return;
            }

            var prop = vm.GetType().GetProperty("RefreshCmd", BindingFlags.Instance | BindingFlags.Public);
            var cmd = prop?.GetValue(vm, null) as System.Windows.Input.ICommand;
            if (cmd?.CanExecute(null) == true) cmd.Execute(null);
        }
    }
}
