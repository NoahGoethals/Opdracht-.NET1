using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddExerciseWindow : Window
    {
        public Exercise? Result { get; private set; }

        public AddExerciseWindow(Exercise? existing = null)
        {
            InitializeComponent();
            if (existing != null)
            {
                NameBox.Text = existing.Name;
                CatBox.Text = existing.Category;
                Result = new Exercise { Id = existing.Id }; 
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result ??= new Exercise();
            Result.Name = NameBox.Text.Trim();
            Result.Category = string.IsNullOrWhiteSpace(CatBox.Text) ? null : CatBox.Text.Trim();
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
