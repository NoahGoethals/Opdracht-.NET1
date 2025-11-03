using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App.View
{
    public partial class ExercisesView : UserControl
    {
        private readonly ExercisesViewModel _vm;

        public ExercisesView()
        {
            InitializeComponent();
            _vm = App.HostApp.Services.GetRequiredService<ExercisesViewModel>();
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
