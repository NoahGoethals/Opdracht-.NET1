// Dialoogvenster voor het toevoegen/bewerken van een Workout (Titel + Datum).

using System;
using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddWorkoutWindow : Window
    {
        // Result: de nieuw aangemaakte of bewerkte Workout (null bij annuleren).
        public Workout? Result { get; private set; }

        // Constructor: optioneel bestaande workout meegeven om te editen; anders nieuw met default datum (vandaag).
        public AddWorkoutWindow(Workout? existing = null)
        {
            InitializeComponent();

            if (existing is null)
            {
                Title = "Nieuwe workout";
                DatePick.SelectedDate = DateTime.Today;   // default datum
            }
            else
            {
                Title = "Workout bewerken";
                TitleBox.Text = existing.Title;
                DatePick.SelectedDate = existing.ScheduledOn;

                // Result vooraf vullen met Id (zodat we bij OK weten dat dit een update is).
                Result = new Workout
                {
                    Id = existing.Id,
                    Title = existing.Title,
                    ScheduledOn = existing.ScheduledOn
                };
            }
        }

        // OK-klik: validatie + waarden overnemen in Result en dialoog sluiten met DialogResult = true.
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

        // Annuleren: sluit zonder wijzigingen.
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
