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
        private readonly int _workoutId;

        private readonly ObservableCollection<Exercise> _available = new();
        private readonly ObservableCollection<WorkoutExercise> _inWorkout = new();

        public EditWorkoutExercisesWindow(int workoutId)
        {
            InitializeComponent();
            _workoutId = workoutId;

            dgInWorkout.ItemsSource = _inWorkout;

            Loaded += async (_, __) => await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                using var scope = App.HostApp.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var all = await ctx.Exercises
                                   .Where(e => !e.IsDeleted)
                                   .OrderBy(e => e.Name)
                                   .ToListAsync();
                _available.Clear();
                foreach (var e in all) _available.Add(e);
                lbAvailable.ItemsSource = _available;

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

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            var selected = lbAvailable.SelectedItem as Exercise;
            if (selected is null) return;

            var repsDlg = new AskRepsWindow();
            if (repsDlg.ShowDialog() != true) return;
            var reps = repsDlg.Reps;

            var weightDlg = new AskWeightWindow();
            if (weightDlg.ShowDialog() != true) return;
            var weight = weightDlg.WeightKg ?? 0;

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

            var sorted = _inWorkout.OrderBy(x => x.Exercise.Name).ToList();
            _inWorkout.Clear();
            foreach (var we in sorted) _inWorkout.Add(we);
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (dgInWorkout.SelectedItem is WorkoutExercise sel)
            {
                _inWorkout.Remove(sel);
            }
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = App.HostApp.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var dbItems = await ctx.WorkoutExercises
                                       .Where(we => we.WorkoutId == _workoutId)
                                       .ToListAsync();

                foreach (var dbWe in dbItems)
                {
                    if (!_inWorkout.Any(x => x.ExerciseId == dbWe.ExerciseId))
                        ctx.WorkoutExercises.Remove(dbWe);
                }

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
