// WorkoutsView (bootstrap): resolveert de ViewModel via DI en triggert de initiële load zodra de view geladen is.

using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App.View
{
    public partial class WorkoutsView : UserControl
    {
        // Houdt de gekoppelde ViewModel bij (via DI container).
        private readonly WorkoutsViewModel _vm;

        // Initialiseert component + koppelt VM + eerste LoadAsync bij Loaded.
        public WorkoutsView()
        {
            InitializeComponent();
            _vm = App.HostApp.Services.GetRequiredService<WorkoutsViewModel>();
            DataContext = _vm;
            Loaded += async (_, __) => await _vm.LoadAsync();
        }
    }
}
