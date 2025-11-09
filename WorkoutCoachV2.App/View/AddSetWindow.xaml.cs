// Venster voor het toevoegen/bewerken van een SessionSet (Exercise, Reps, Weight, RPE, Notes).

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddSetWindow : Window
    {
        // Resultaat van het venster: ingevulde SessionSet (null bij annuleren).
        public SessionSet? Result { get; private set; }

        // Beschikbare oefeningen voor de ComboBox.
        private readonly List<Exercise> _exercises;

        // Bestaande set (bij bewerken); null bij toevoegen.
        private readonly SessionSet? _existing;

        // Constructor: krijgt lijst oefeningen en optioneel bestaande set om te editen.
        public AddSetWindow(IEnumerable<Exercise> exercises, SessionSet? existing = null)
        {
            InitializeComponent();

            _exercises = exercises.ToList();
            _existing = existing;

            // ComboBox vullen en tonen op naam.
            ExerciseBox.ItemsSource = _exercises;
            ExerciseBox.DisplayMemberPath = nameof(Exercise.Name);

            // Prefill bij bewerken, defaults bij toevoegen.
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

        // OK: validatie + waarden overzetten naar Result; dialoog afsluiten met OK.
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            // Exercise verplicht
            if (ExerciseBox.SelectedItem is not Exercise ex)
            {
                MessageBox.Show("Kies een oefening.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Reps: positief geheel getal
            if (!int.TryParse(RepsBox.Text, out var reps) || reps <= 0)
            {
                MessageBox.Show("Reps moet een positief getal zijn.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Weight: niet-negatief getal
            if (!double.TryParse(WeightBox.Text, out var weight) || weight < 0)
            {
                MessageBox.Show("Gewicht is ongeldig.", "Set", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // RPE: optioneel, integer
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

            // Model opbouwen (nieuw of bestaande updaten)
            var model = _existing ?? new SessionSet();
            model.ExerciseId = ex.Id;
            model.Reps = reps;
            model.Weight = weight;
            model.Rpe = rpe;
            model.Note = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

            Result = model;
            DialogResult = true;
        }

        // Annuleer: sluit zonder wijzigingen.
        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
