using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App.View
{
    public partial class WorkoutsView : UserControl
    {
        private readonly WorkoutsViewModel _vm;

        public WorkoutsView()
        {
            InitializeComponent();
            _vm = App.HostApp.Services.GetRequiredService<WorkoutsViewModel>();
            DataContext = _vm;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await _vm.LoadAsync();
        }
    }
}
