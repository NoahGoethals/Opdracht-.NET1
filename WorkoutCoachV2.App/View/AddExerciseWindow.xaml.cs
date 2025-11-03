using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddExerciseWindow : Window
    {
        public Exercise? Result { get; private set; }

        public AddExerciseWindow(Exercise? model = null)
        {
            InitializeComponent();
            if (model != null)
            {
                NameBox.Text = model.Name;
                CatBox.Text = model.Category;
                Result = new Exercise { Id = model.Id }; 
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result ??= new Exercise();
            Result.Name = NameBox.Text.Trim();
            Result.Category = string.IsNullOrWhiteSpace(CatBox.Text) ? null : CatBox.Text.Trim();

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
