// Beheer de inhoud van een workout (oefeningen toevoegen/verwijderen, reps/gewicht bewerken).
// - Links laden we alle beschikbare oefeningen (exclusief soft-deleted)
// - Rechts tonen/bewerken we de set WorkoutExercise records voor de gekozen workout
// - Opslaan synchroniseert de rechterlijst met de database (toevoegen/updaten/verwijderen)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WorkoutCoachV2.Model;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class EditWorkoutExercisesWindow : Window
    {
        // WORKOUT-ID (doelrecord)
        private readonly int _workoutId;

        // LINKERLIJST: beschikbare oefeningen
        private readonly ObservableCollection<Exercise> _available = new();

        // RECHTERLIJST: oefeningen die in de workout zitten (bewerkbaar)
        private readonly ObservableCollection<WorkoutExercise> _inWorkout = new();

        // CONSTRUCTOR: set bindings en laad data bij opstart
        public EditWorkoutExercisesWindow(int workoutId)
        {
            InitializeComponent();
            _workoutId = workoutId;

            // Bind rechterraster aan in-memory collectie
            dgInWorkout.ItemsSource = _inWorkout;

            // Laad initieel de data
            Loaded += async (_, __) => await LoadDataAsync();
        }

        // DATA LADEN: beschikbare oefeningen + huidige workout-inhoud
        private async Task LoadDataAsync()
        {
            try
            {
                using var scope = App.HostApp.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Beschikbaar (niet soft-deleted), alfabetisch op naam
                var all = await ctx.Exercises
                                   .Where(e => !e.IsDeleted)
                                   .OrderBy(e => e.Name)
                                   .ToListAsync();
                _available.Clear();
                foreach (var e in all) _available.Add(e);
                lbAvailable.ItemsSource = _available;

                // Inhoud van de workout (inclusief Exercise voor naamweergave)
                var current = await ctx.WorkoutExercises
                                       .Include(we => we.Exercise)
                                       .Where(we => we.WorkoutId == _workoutId)
                                       .OrderBy(we => we.Exercise.Name)
                                       .ToListAsync();
                _inWorkout.Clear();
                foreach (var we in current) _inWorkout.Add(we);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Kon data niet laden:\n{ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ZOEK: filter beschikbare oefeningen op naam (case-insensitive)
        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var term = (txtSearch.Text ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(term))
            {
                lbAvailable.ItemsSource = _available;
                return;
            }

            var filtered = _available.Where(x => x.Name.ToLower().Contains(term)).ToList();
            lbAvailable.ItemsSource = filtered;
        }

        // TOEVOEGEN: vraag Reps + Gewicht, voeg toe of update in rechterlijst; her-sorteer op naam
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selected = lbAvailable.SelectedItem as Exercise;
            if (selected is null) return;

            // 1) Reps vragen
            var repsDlg = new AskRepsWindow();
            if (repsDlg.ShowDialog() != true) return;
            var reps = repsDlg.Reps;

            // 2) Gewicht vragen
            var weightDlg = new AskWeightWindow();
            if (weightDlg.ShowDialog() != true) return;
            var weight = weightDlg.WeightKg ?? 0;

            // 3) Toevoegen of updaten in de rechterlijst
            var existing = _inWorkout.FirstOrDefault(x => x.ExerciseId == selected.Id);
            if (existing is null)
            {
                _inWorkout.Add(new WorkoutExercise
                {
                    WorkoutId = _workoutId,
                    ExerciseId = selected.Id,
                    Exercise = selected,
                    Reps = reps,
                    WeightKg = weight
                });
            }
            else
            {
                existing.Reps = reps;
                existing.WeightKg = weight;
            }

            // 4) Netjes sorteren op oefeningsnaam
            var sorted = _inWorkout.OrderBy(x => x.Exercise.Name).ToList();
            _inWorkout.Clear();
            foreach (var we in sorted) _inWorkout.Add(we);
        }

        // VERWIJDEREN (alleen uit de rechterlijst; persistente delete gebeurt bij Bewaren)
        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (dgInWorkout.SelectedItem is WorkoutExercise sel)
            {
                _inWorkout.Remove(sel);
            }
        }

        // BEWAREN: synchroniseer rechterlijst met DB (verwijder niet-meer-aanwezige, voeg nieuwe toe, update reps/gewicht)
        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = App.HostApp.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Bestaande DB-records voor deze workout
                var dbItems = await ctx.WorkoutExercises
                                       .Where(we => we.WorkoutId == _workoutId)
                                       .ToListAsync();

                // 1) Verwijderen wat verdwenen is uit de rechterlijst
                foreach (var dbWe in dbItems)
                {
                    if (!_inWorkout.Any(x => x.ExerciseId == dbWe.ExerciseId))
                        ctx.WorkoutExercises.Remove(dbWe);
                }

                // 2) Toevoegen of bijwerken
                foreach (var item in _inWorkout)
                {
                    var dbWe = dbItems.FirstOrDefault(x => x.ExerciseId == item.ExerciseId);
                    if (dbWe is null)
                    {
                        ctx.WorkoutExercises.Add(new WorkoutExercise
                        {
                            WorkoutId = _workoutId,
                            ExerciseId = item.ExerciseId,
                            Reps = item.Reps,
                            WeightKg = item.WeightKg
                        });
                    }
                    else
                    {
                        dbWe.Reps = item.Reps;
                        dbWe.WeightKg = item.WeightKg;
                        ctx.WorkoutExercises.Update(dbWe);
                    }
                }

                await ctx.SaveChangesAsync();
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Bewaren mislukt:\n{ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
