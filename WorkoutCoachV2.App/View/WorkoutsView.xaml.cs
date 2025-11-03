using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App.View
{
    public partial class WorkoutsView : UserControl
    {
        public WorkoutsView()
        {
            InitializeComponent();
            DataContext = App.HostApp.Services.GetRequiredService<WorkoutsViewModel>();
        }
    }
}
