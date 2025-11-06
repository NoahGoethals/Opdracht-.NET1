using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WorkoutCoachV2.Model.Data;      
using WorkoutCoachV2.Model.Models;    

namespace WorkoutCoachV2.App.View
{
    public partial class SessionsView : UserControl
    {
        private async void btnNewFromWorkouts_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            var dlg = new NewSessionFromWorkoutsWindow { Owner = owner };
            var ok = dlg.ShowDialog() == true;
            if (ok) await TryRefreshAsync();
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = dgSessions?.SelectedItem;
            if (selected == null)
            {
                MessageBox.Show("Selecteer eerst een sessie.", "Verwijderen",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var idProp = selected.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
            if (idProp == null || idProp.GetValue(selected) is not int id)
            {
                MessageBox.Show("Kan het ID van de sessie niet bepalen.", "Verwijderen",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Sessie verwijderen?", "Bevestigen",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var sets = await db.SessionSets.Where(s => s.SessionId == id).ToListAsync();
            if (sets.Count > 0) db.SessionSets.RemoveRange(sets);

            var session = await db.Sessions.FirstOrDefaultAsync(s => s.Id == id);
            if (session != null) db.Sessions.Remove(session);

            await db.SaveChangesAsync();
            await TryRefreshAsync();
        }

        private async Task TryRefreshAsync()
        {
            var vm = DataContext;
            if (vm == null) return;

            var mi = vm.GetType().GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.Public);
            if (mi != null)
            {
                if (mi.Invoke(vm, null) is Task t) await t;
                return;
            }

            var prop = vm.GetType().GetProperty("RefreshCmd", BindingFlags.Instance | BindingFlags.Public);
            var cmd = prop?.GetValue(vm, null) as System.Windows.Input.ICommand;
            if (cmd?.CanExecute(null) == true) cmd.Execute(null);
        }
    }
}
