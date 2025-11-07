using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace WorkoutCoachV2.App.View
{
    public partial class SessionsView : UserControl
    {
        private void btnDetails_Click(object sender, RoutedEventArgs e)
        {
            var row = dgSessions?.SelectedItem;
            if (row is null)
            {
                MessageBox.Show("Selecteer eerst een sessie.", "Inhoud",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            int id = 0;
            var t = row.GetType();
            var p = t.GetProperty("Id") ?? t.GetProperty("SessionId");
            if (p != null && p.GetValue(row) is int v) id = v;

            if (id <= 0)
            {
                MessageBox.Show("Kon het ID van de sessie niet bepalen.", "Inhoud",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var owner = Window.GetWindow(this);
            var dlg = new SessionDetailsWindow(id) { Owner = owner };
            dlg.ShowDialog();
        }
    }
}
