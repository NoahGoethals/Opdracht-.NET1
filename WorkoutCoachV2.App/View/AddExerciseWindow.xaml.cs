// Dialoogvenster om een Exercise toe te voegen of te bewerken (Naam + optionele Categorie).

using System.Windows;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class AddExerciseWindow : Window
    {
        // Result: de ingevulde/gewijzigde Exercise na 'Bewaar' (null bij annuleren).
        public Exercise? Result { get; private set; }

        // Constructor: optioneel bestaande Exercise meegeven om te bewerken (prefill velden).
        public AddExerciseWindow(Exercise? existing = null)
        {
            InitializeComponent();

            // Prefill bij bewerken en Result alvast koppelen aan bestaande Id.
            if (existing != null)
            {
                NameBox.Text = existing.Name;
                CatBox.Text = existing.Category;
                Result = new Exercise { Id = existing.Id };
            }
        }

        // 'Bewaar': velden uitlezen en Result invullen; sluit dialoog met OK.
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Result ??= new Exercise();
            Result.Name = NameBox.Text.Trim();
            Result.Category = string.IsNullOrWhiteSpace(CatBox.Text) ? null : CatBox.Text.Trim();
            DialogResult = true;
        }

        // 'Annuleer': sluit zonder wijzigingen.
        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();
    }
}
