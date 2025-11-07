using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WorkoutCoachV2.App.View
{
    public partial class SessionsView : UserControl
    {
        public SessionsView()
        {
            InitializeComponent();
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await TryRefreshAsync();
        }
    }
}
