// Doel: eenvoudige dialoog om snel een losse trainingsset (Session + SessionSet) toe te voegen.
// Flow: laad alle oefeningen -> optionele filter -> gebruiker kiest oefening + vult gewicht/reps/datum/PR -> bewaar.

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
        // Lokale cache met alle oefeningen + view voor filtering
        private List<Exercise> _allExercises = new();
        private ICollectionView? _view;

        // Constructor: init UI, laad data bij 'Loaded'
        public NewStatEntryWindow()
        {
            InitializeComponent();
            Loaded += NewStatEntryWindow_Loaded;
        }

        // Data laden: oefeningen in lijst + standaarddatum vandaag
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

        // Zoeken: filter op naam (case-insensitive)
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

        // Bewaar: valideer invoer, maak Session + SessionSet aan, sla op
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validatie: een oefening moet gekozen zijn
            if (lbExercises.SelectedItem is not Exercise ex)
            {
                MessageBox.Show("Kies een oefening.", "Nieuwe set", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Parse gewicht + reps (non-negatieve defaults)
            if (!double.TryParse(tbWeight.Text.Trim(), out var weight) || weight < 0) weight = 0;
            if (!int.TryParse(tbReps.Text.Trim(), out var reps) || reps < 0) reps = 0;
            var date = dpDate.SelectedDate ?? DateTime.Today;
            var isPr = cbPr.IsChecked == true;

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Maak een eenvoudige Session (titel verwijst naar oefening)
            var session = new Session
            {
                Title = $"Losse set: {ex.Name}",
                Date = date.Date,
                Description = null
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync(); // Id nodig voor SessionSet

            // Maak bijhorende SessionSet
            var set = new SessionSet
            {
                SessionId = session.Id,
                ExerciseId = ex.Id,
                Weight = weight,
                Reps = reps
            };

            // Optioneel PR-vlag (propertynaam kan variëren in jouw model)
            var prProp = set.GetType().GetProperty("IsPr") ?? set.GetType().GetProperty("Pr");
            if (prProp != null) prProp.SetValue(set, isPr);

            db.SessionSets.Add(set);
            await db.SaveChangesAsync();

            DialogResult = true;
        }
    }
}
