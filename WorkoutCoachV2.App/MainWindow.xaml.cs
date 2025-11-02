using System.Windows;
using WorkoutCoachV2.App.ViewModels;

namespace WorkoutCoachV2.App
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
