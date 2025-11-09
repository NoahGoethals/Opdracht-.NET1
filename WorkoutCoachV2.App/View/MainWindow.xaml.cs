// Hoofdvenster van de app:
// - Zet DataContext naar MainViewModel (rolgebaseerde UI)
// - Laadt tab-inhoud via DI (Exercises/Workouts/Sessions) en direct (Stats)
// - Handlers voor Logout en het openen van het Users-venster

using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.View;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App
{
    public partial class MainWindow : Window
    {
        // Initialiseert UI en injecteert de verschillende tab-views vanuit DI
        public MainWindow()
        {
            InitializeComponent();

            // Design-mode guard (geen DI in designer)
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            var sp = App.HostApp.Services;

            // Hoofd-VM met rolgebaseerde zichtbaarheid/tekst
            DataContext = sp.GetRequiredService<MainViewModel>();

            // Tabcontent aanleveren: we gebruiken bestaande viewmodels uit DI
            var exVm = sp.GetRequiredService<ExercisesViewModel>();
            var wkVm = sp.GetRequiredService<WorkoutsViewModel>();
            var ssVm = sp.GetRequiredService<SessionsViewModel>();

            ExercisesHost.Content = new ExercisesView { DataContext = exVm };
            WorkoutsHost.Content = new WorkoutsView { DataContext = wkVm };
            SessionsHost.Content = new SessionsView { DataContext = ssVm };

            // Stats heeft zijn eigen interne data-ophaalmechanisme; direct instantiëren is voldoende
            StatsHost.Content = new StatsView();
        }

        // Uitloggen: terug naar LoginWindow
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            var login = App.HostApp.Services.GetRequiredService<LoginWindow>();
            login.Show();
            Close();
        }

        // Alleen zichtbaar voor Admin: opent gebruikersbeheer
        private void OpenUsers_Click(object sender, RoutedEventArgs e)
        {
            var wnd = App.HostApp.Services.GetRequiredService<UserAdminWindow>();
            wnd.Owner = this;
            wnd.ShowDialog();
        }
    }
}
