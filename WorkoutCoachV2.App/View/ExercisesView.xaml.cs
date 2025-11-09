// UI voor het beheren van oefeningen (bindt naar ExercisesViewModel)
// - Haalt de VM via DI uit App.HostApp.Services
// - Laadt data éénmalig bij het tonen van de view

using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App.View
{
    public partial class ExercisesView : UserControl
    {
        // ViewModel-instantie uit DI-container
        private readonly ExercisesViewModel _vm;

        // Constructor: initialiseer component, resolve VM en zet DataContext
        public ExercisesView()
        {
            InitializeComponent();
            _vm = App.HostApp.Services.GetRequiredService<ExercisesViewModel>();
            DataContext = _vm;
            Loaded += OnLoaded; // eerste keer data laden
        }

        // Bij eerste Load: ontkoppel handler en laad de lijst
        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            await _vm.LoadAsync();
        }
    }
}
