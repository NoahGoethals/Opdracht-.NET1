// Sessie-detailvenster: laadt sessie + sets; toont grid; vult leegte uit laatste workout; bewaart header-velden bij sluiten.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class SessionDetailsWindow : Window
    {
        // Velden: geselecteerde sessie-id en modelbuffer
        private readonly int _sessionId;
        private Session? _session;

        // Grid-rijen (viewmodelletje per set)
        private readonly ObservableCollection<SessionSetRow> _rows = new();

        // Constructor: id aanvaarden, grid binden, lazy load
        public SessionDetailsWindow(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;

            dgSets.ItemsSource = _rows;
            Loaded += async (_, __) => await LoadAsync();
        }

        // Data laden: sessie + sets; of import uit meest recente workout
        private async Task LoadAsync()
        {
            try
            {
                using var scope = App.HostApp.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                _session = await ctx.Sessions
                    .Include(s => s.Sets)
                        .ThenInclude(ss => ss.Exercise)
                    .FirstOrDefaultAsync(s => s.Id == _sessionId);

                if (_session == null)
                {
                    MessageBox.Show(this, "Sessie niet gevonden.", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }

                // Header vullen
                txtTitle.Text = _session.Title;
                dpDate.SelectedDate = _session.Date;
                txtDescription.Text = _session.Description ?? string.Empty;

                // Rijen voorbereiden
                _rows.Clear();

                if (_session.Sets != null && _session.Sets.Count > 0)
                {
                    // Bestaande sets tonen
                    foreach (var ss in _session.Sets.OrderBy(x => x.Exercise.Name))
                    {
                        _rows.Add(new SessionSetRow
                        {
                            ExerciseId = ss.ExerciseId,
                            ExerciseName = ss.Exercise?.Name ?? "-",
                            Reps = ss.Reps,
                            Weight = ss.Weight
                        });
                    }
                }
                else
                {
                    // Geen sets: automatisch overnemen uit laatste workout
                    await ImportFromLatestWorkoutAsync(ctx);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Kon sessiedetails niet laden:\n{ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Hulpmethode: haal items uit meest recente workout (op Id aflopend) en vul grid
        private async Task ImportFromLatestWorkoutAsync(AppDbContext ctx)
        {
            var latestWorkout = await ctx.Workouts
                .OrderByDescending(w => w.Id)
                .FirstOrDefaultAsync();

            if (latestWorkout == null)
                return;

            var items = await ctx.WorkoutExercises
                .Include(we => we.Exercise)
                .Where(we => we.WorkoutId == latestWorkout.Id)
                .OrderBy(we => we.Exercise.Name)
                .ToListAsync();

            _rows.Clear();
            foreach (var we in items)
            {
                _rows.Add(new SessionSetRow
                {
                    ExerciseId = we.ExerciseId,
                    ExerciseName = we.Exercise?.Name ?? "-",
                    Reps = we.Reps,
                    Weight = we.WeightKg ?? 0
                });
            }
        }

        // Sluiten: header-velden (titel/desc/datum) wegschrijven; grid-edit blijft in-memory (geen CRUD hier)
        private async void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var scope = App.HostApp.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var db = await ctx.Sessions.FirstOrDefaultAsync(s => s.Id == _sessionId);
                if (db != null)
                {
                    db.Title = (txtTitle.Text ?? string.Empty).Trim();
                    db.Description = txtDescription.Text ?? string.Empty;
                    if (dpDate.SelectedDate.HasValue)
                        db.Date = dpDate.SelectedDate.Value;

                    await ctx.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Bewaren mislukt:\n{ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Close();
        }

        // Eenvoudige weergavemodel-rij voor het DataGrid
        private sealed class SessionSetRow
        {
            public int ExerciseId { get; set; }
            public string ExerciseName { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
        }
    }
}
