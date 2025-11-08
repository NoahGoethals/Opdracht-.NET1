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
        private readonly int _sessionId;
        private Session? _session;

        private readonly ObservableCollection<SessionSetRow> _rows = new();

        public SessionDetailsWindow(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;

            dgSets.ItemsSource = _rows;
            Loaded += async (_, __) => await LoadAsync();
        }

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

                txtTitle.Text = _session.Title;
                dpDate.SelectedDate = _session.Date;
                txtDescription.Text = _session.Description ?? string.Empty;

                _rows.Clear();

                if (_session.Sets != null && _session.Sets.Count > 0)
                {
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
                    await ImportFromLatestWorkoutAsync(ctx);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Kon sessiedetails niet laden:\n{ex.Message}", "Fout", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private sealed class SessionSetRow
        {
            public int ExerciseId { get; set; }
            public string ExerciseName { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
        }
    }
}
