using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddSetWindow : Window
    {
        public SessionSet? Result { get; private set; }
        private readonly List<Exercise> _exercises;
        private readonly SessionSet? _existing;

        public AddSetWindow(IEnumerable<Exercise> exercises, SessionSet? existing = null)
        {
            InitializeComponent();
            _exercises = exercises.ToList();
            _existing = existing;

            ExerciseBox.ItemsSource = _exercises;
            ExerciseBox.DisplayMemberPath = nameof(Exercise.Name);

            if (_existing != null)
            {
                ExerciseBox.SelectedItem = _exercises.FirstOrDefault(e => e.Id == _existing.ExerciseId);
                RepsBox.Text = _existing.Reps.ToString();
                WeightBox.Text = _existing.Weight.ToString();
                RpeBox.Text = _existing.Rpe?.ToString() ?? "";
                NotesBox.Text = _existing.Note ?? "";
            }
            else
            {
                ExerciseBox.SelectedIndex = _exercises.Any() ? 0 : -1;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (ExerciseBox.SelectedItem is not Exercise ex)
            {
                MessageBox.Show("Kies een oefening.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(RepsBox.Text, out var reps) || reps <= 0)
            {
                MessageBox.Show("Reps moet een positief getal zijn.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!double.TryParse(WeightBox.Text, out var weight) || weight < 0)
            {
                MessageBox.Show("Gewicht is ongeldig.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int? rpe = null;
            if (!string.IsNullOrWhiteSpace(RpeBox.Text))
            {
                if (int.TryParse(RpeBox.Text, out var r)) rpe = r;
                else
                {
                    MessageBox.Show("RPE is ongeldig.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var model = _existing ?? new SessionSet();
            model.ExerciseId = ex.Id;
            model.Reps = reps;
            model.Weight = weight;
            model.Rpe = rpe;
            model.Note = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim(); 

            Result = model;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
