using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.View;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            var sp = App.HostApp.Services;

            DataContext = sp.GetRequiredService<MainViewModel>();

            var exVm = sp.GetRequiredService<ExercisesViewModel>();
            var wkVm = sp.GetRequiredService<WorkoutsViewModel>();
            var ssVm = sp.GetRequiredService<SessionsViewModel>();

            ExercisesHost.Content = new ExercisesView { DataContext = exVm };
            WorkoutsHost.Content = new WorkoutsView { DataContext = wkVm };
            SessionsHost.Content = new SessionsView { DataContext = ssVm };
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

            var login = App.HostApp.Services.GetRequiredService<LoginWindow>();
            login.Show();
            Close();
        }
    }
}
