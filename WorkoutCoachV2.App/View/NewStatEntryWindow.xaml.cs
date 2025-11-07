using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class NewStatEntryWindow : Window
    {
        private List<Exercise> _allExercises = new();
        private ICollectionView? _view;

        public NewStatEntryWindow()
        {
            InitializeComponent();
            Loaded += NewStatEntryWindow_Loaded;
        }

        private async void NewStatEntryWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            _allExercises = await db.Exercises.AsNoTracking()
                .OrderBy(e => e.Name).ToListAsync();

            lbExercises.ItemsSource = _allExercises;
            _view = CollectionViewSource.GetDefaultView(lbExercises.ItemsSource);
            dpDate.SelectedDate = DateTime.Today;

            if (_allExercises.Count > 0) lbExercises.SelectedIndex = 0;
        }

        private void tbSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_view == null) return;
            var q = tbSearch.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(q))
            {
                _view.Filter = null;
            }
            else
            {
                _view.Filter = o =>
                {
                    var ex = (Exercise)o;
                    return ex.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
                };
            }
            _view.Refresh();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (lbExercises.SelectedItem is not Exercise ex)
            {
                MessageBox.Show("Kies een oefening.", "Nieuwe set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!double.TryParse(tbWeight.Text.Trim(), out var weight) || weight < 0) weight = 0;
            if (!int.TryParse(tbReps.Text.Trim(), out var reps) || reps < 0) reps = 0;
            var date = dpDate.SelectedDate ?? DateTime.Today;
            var isPr = cbPr.IsChecked == true;

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = new Session
            {
                Title = $"Losse set: {ex.Name}",
                Date = date.Date,
                Description = null
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            var set = new SessionSet
            {
                SessionId = session.Id,
                ExerciseId = ex.Id,
                Weight = weight,
                Reps = reps
            };

            var prProp = set.GetType().GetProperty("IsPr") ?? set.GetType().GetProperty("Pr");
            if (prProp != null) prProp.SetValue(set, isPr);

            db.SessionSets.Add(set);
            await db.SaveChangesAsync();

            DialogResult = true;
        }
    }
}
