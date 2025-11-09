// SessionsView (code-behind): init + Vernieuwen-knop. Overige handlers zitten in de andere partials.
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WorkoutCoachV2.App.View
{
    public partial class SessionsView : UserControl
    {
        // Init van de XAML-component.
        public SessionsView()
        {
            InitializeComponent();
        }

        // Vernieuwen-knop -> herlaadt de VM-lijst (via TryRefreshAsync uit actions-partial).
        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await TryRefreshAsync();
        }
    }
}
