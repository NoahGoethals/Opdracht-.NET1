using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkoutCoachV2.Model.Data;       
using WorkoutCoachV2.Model.Models;    

namespace WorkoutCoachV2.App.View
{
    public partial class NewSessionFromWorkoutsWindow : Window
    {
        private ObservableCollection<SelectableWorkout> _items = new();

        public NewSessionFromWorkoutsWindow()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var workouts = await db.Workouts
                .AsNoTracking()
                .OrderByDescending(w => w.ScheduledOn)
                .ThenByDescending(w => w.CreatedAt)
                .ToListAsync();

            _items = new ObservableCollection<SelectableWorkout>(
                workouts.Select(w => new SelectableWorkout { Workout = w }));

            dgWorkouts.ItemsSource = _items;

            dpDate.SelectedDate = DateTime.Today;
            tbTitle.Text = "Nieuwe sessie";
            tbDescription.Text = string.Empty;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var title = tbTitle.Text?.Trim();
            var date = dpDate.SelectedDate ?? DateTime.Today;
            var description = tbDescription.Text?.Trim() ?? string.Empty;

            var chosen = _items.Where(x => x.IsSelected).Select(x => x.Workout).ToList();
            if (chosen.Count == 0)
            {
                MessageBox.Show("Selecteer minstens één workout.", "Nieuwe sessie",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("Geef een titel in.", "Nieuwe sessie",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var ids = chosen.Select(w => w.Id).ToList();
            var workouts = await db.Workouts
                .Where(w => ids.Contains(w.Id))
                .Include(w => w.Exercises)               
                    .ThenInclude(we => we.Exercise)
                .ToListAsync();

            var session = new Session
            {
                Title = title!,
                Date = date.Date,
                Description = description
            };
            db.Sessions.Add(session);
            await db.SaveChangesAsync(); 

            var sets = new List<SessionSet>();
            foreach (var w in workouts)
            {
                foreach (var we in w.Exercises)
                {
                    sets.Add(new SessionSet
                    {
                        SessionId = session.Id,
                        ExerciseId = we.ExerciseId,
                        Reps = we.Reps,
                        Weight = 0 
                    });
                }
            }

            if (sets.Count > 0)
            {
                db.SessionSets.AddRange(sets);
                await db.SaveChangesAsync();
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private class SelectableWorkout
        {
            public bool IsSelected { get; set; }
            public Workout Workout { get; set; } = default!;
        }
    }
}
