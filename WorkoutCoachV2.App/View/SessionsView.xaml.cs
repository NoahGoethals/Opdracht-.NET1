using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App.View
{
    public partial class SessionsView : UserControl
    {
        public SessionsView()
        {
            InitializeComponent();
            DataContext = App.HostApp.Services.GetRequiredService<SessionsViewModel>();
        }
    }
}
