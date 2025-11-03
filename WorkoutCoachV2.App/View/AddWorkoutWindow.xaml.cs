using System;
using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddWorkoutWindow : Window
    {
        public Workout? Result { get; private set; }

        public AddWorkoutWindow(Workout? existing = null)
        {
            InitializeComponent();
            if (existing is null)
            {
                Title = "Nieuwe workout";
                DatePick.SelectedDate = DateTime.Today;
            }
            else
            {
                Title = "Workout bewerken";
                TitleBox.Text = existing.Title;
                DatePick.SelectedDate = existing.ScheduledOn;
                Result = new Workout
                {
                    Id = existing.Id,
                    Title = existing.Title,
                    ScheduledOn = existing.ScheduledOn
                };
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleBox.Text) || DatePick.SelectedDate is null)
            {
                MessageBox.Show("Titel en datum zijn verplicht.", "Workout",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Result ??= new Workout();
            Result.Title = TitleBox.Text.Trim();
            Result.ScheduledOn = DatePick.SelectedDate!.Value;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
