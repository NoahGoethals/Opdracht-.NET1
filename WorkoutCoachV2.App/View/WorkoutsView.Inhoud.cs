using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WorkoutCoachV2.App.View
{
    public partial class WorkoutsView : UserControl
    {
        private async void btnInhoud_Click(object sender, RoutedEventArgs e)
        {
            object selectedObj = null;
            var vm = DataContext;
            var selProp = vm?.GetType().GetProperty("Selected", BindingFlags.Instance | BindingFlags.Public);
            selectedObj = selProp?.GetValue(vm) ?? dgWorkouts?.SelectedItem;

            if (selectedObj == null)
            {
                MessageBox.Show("Selecteer eerst een workout.", "Inhoud beheren",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int workoutId = TryGetId(selectedObj);
            if (workoutId <= 0)
            {
                MessageBox.Show("Kon de Id van de geselecteerde workout niet bepalen.", "Inhoud beheren",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var owner = Window.GetWindow(this);
            var dlg = new EditWorkoutExercisesWindow(workoutId)
            {
                Owner = owner
            };

            var ok = dlg.ShowDialog() == true;
            if (ok)
            {
                await TryRefreshAsync();
            }
        }

        private static int TryGetId(object selected)
        {
            var type = selected.GetType();
            var idProp = type.GetProperty("Id") ?? type.GetProperty("WorkoutId");
            if (idProp == null) return 0;

            var value = idProp.GetValue(selected);
            if (value == null) return 0;

            try
            {
                return System.Convert.ToInt32(value);
            }
            catch
            {
                return 0;
            }
        }

        private async Task TryRefreshAsync()
        {
            var vm = DataContext;
            if (vm == null) return;

            // Zoek naar LoadAsync()
            var mi = vm.GetType().GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                var result = mi.Invoke(vm, null);
                if (result is Task t) await t;
                return;
            }

            // Of voer RefreshCmd uit
            var prop = vm.GetType().GetProperty("RefreshCmd", BindingFlags.Instance | BindingFlags.Public);
            var cmd = prop?.GetValue(vm, null) as System.Windows.Input.ICommand;
            if (cmd?.CanExecute(null) == true) cmd.Execute(null);
        }
    }
}
