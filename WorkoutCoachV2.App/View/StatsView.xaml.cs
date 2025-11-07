using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WorkoutCoachV2.Model.Data;     
using WorkoutCoachV2.Model.Models;  

namespace WorkoutCoachV2.App.View
{
    public partial class StatsView : UserControl
    {
        public StatsView()
        {
            InitializeComponent();
            Loaded += async (_, __) =>
            {
                dpFrom.SelectedDate = DateTime.Today.AddDays(-7);
                dpTo.SelectedDate = DateTime.Today;
                await CalculateAsync();
            };
        }

        private async void btnBereken_Click(object sender, RoutedEventArgs e)
            => await CalculateAsync();

        private static DateTime ReadSessionDate(Session s)
        {
            var t = s.GetType();
            object? v =
                t.GetProperty("Date")?.GetValue(s) ??
                t.GetProperty("ScheduledOn")?.GetValue(s) ??
                t.GetProperty("ScheduledAt")?.GetValue(s) ??
                t.GetProperty("PerformedOn")?.GetValue(s) ??
                t.GetProperty("CreatedAt")?.GetValue(s);

            return v is DateTime dt ? dt : DateTime.MinValue;
        }

        private static double Value(SessionSet ss) => Convert.ToDouble(ss.Weight) * ss.Reps;

        private async Task CalculateAsync()
        {
            var from = dpFrom.SelectedDate ?? DateTime.Today.AddDays(-7);
            var to = dpTo.SelectedDate ?? DateTime.Today;

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var allSets = await db.SessionSets
                .Include(s => s.Exercise)
                .Include(s => s.Session)
                .AsNoTracking()
                .ToListAsync();

            var inRange = allSets
                .Where(ss =>
                {
                    var d = ReadSessionDate(ss.Session);
                    return d >= from && d <= to;
                })
                .ToList();

            var weekVolume = inRange.Sum(Value);
            tbWeekVolume.Text = $"Weekvolume: {weekVolume:N0}";

            var bestPerExercise = inRange
                .GroupBy(ss => ss.ExerciseId)
                .Select(g => g
                    .OrderByDescending(Value)
                    .ThenByDescending(ss => ReadSessionDate(ss.Session))
                    .First())
                .ToList();

            var rows = new List<BestRow>();
            foreach (var s in bestPerExercise)
            {
                var d = ReadSessionDate(s.Session);
                var prevBest = allSets
                    .Where(x => x.ExerciseId == s.ExerciseId && ReadSessionDate(x.Session) < d)
                    .Select(Value)
                    .DefaultIfEmpty(0)
                    .Max();

                var v = Value(s);
                rows.Add(new BestRow
                {
                    Exercise = s.Exercise?.Name ?? $"#{s.ExerciseId}",
                    Weight = s.Weight,
                    Reps = s.Reps,
                    Date = d,
                    IsPr = v > prevBest 
                });
            }

            dgBest.ItemsSource = rows
                .OrderBy(r => r.Exercise)
                .ToList();
        }

        private sealed class BestRow
        {
            public string Exercise { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
            public DateTime Date { get; set; }
            public bool IsPr { get; set; }
        }
    }
}
