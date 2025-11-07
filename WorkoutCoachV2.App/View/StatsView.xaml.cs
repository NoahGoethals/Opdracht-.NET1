using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class StatsView : UserControl
    {
        private readonly ObservableCollection<GridRow> _rows = new();

        public StatsView()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private static PropertyInfo? FindPrProp(SessionSet ss)
            => ss.GetType().GetProperty("IsPr") ?? ss.GetType().GetProperty("Pr");

        private static bool ReadPr(SessionSet ss)
        {
            var p = FindPrProp(ss);
            if (p == null) return false;
            var v = p.GetValue(ss);
            return v is bool b && b;
        }

        private static void WritePr(DbContext db, int sessionSetId, bool value)
        {
            var stub = new SessionSet { Id = sessionSetId };
            db.Attach(stub);

            var et = db.Model.FindEntityType(typeof(SessionSet));
            var prProp = et?.FindProperty("IsPr") ?? et?.FindProperty("Pr");
            if (prProp == null) return;

            var entry = db.Entry(stub).Property(prProp.Name);
            entry.CurrentValue = value;
            entry.IsModified = true;
        }

        private async Task LoadAsync()
        {
            _rows.Clear();

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var data = await db.SessionSets
                .AsNoTracking()
                .Include(s => s.Exercise)
                .Include(s => s.Session)
                .OrderByDescending(s => s.Session.Date)
                .ThenBy(s => s.Exercise.Name)
                .ToListAsync();

            foreach (var s in data)
            {
                _rows.Add(new GridRow
                {
                    SessionSetId = s.Id,
                    SessionId = s.SessionId,
                    ExerciseId = s.ExerciseId,
                    ExerciseName = s.Exercise?.Name ?? $"#{s.ExerciseId}",
                    Weight = s.Weight,
                    Reps = s.Reps,
                    Date = s.Session?.Date ?? DateTime.Today,
                    IsPr = ReadPr(s)
                });
            }

            dg.ItemsSource = _rows;
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private async void BtnNewRow_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            var dlg = new NewStatEntryWindow { Owner = owner };
            if (dlg.ShowDialog() == true)
            {
                await LoadAsync();
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var row in _rows)
            {
                var stub = new SessionSet { Id = row.SessionSetId };
                db.Attach(stub);
                stub.Weight = row.Weight;
                stub.Reps = row.Reps;

                WritePr(db, row.SessionSetId, row.IsPr);

                var sessStub = new Session { Id = row.SessionId, Date = row.Date.Date };
                db.Attach(sessStub);
                db.Entry(sessStub).Property(s => s.Date).IsModified = true;
            }

            await db.SaveChangesAsync();
            MessageBox.Show("Opgeslagen.", "Overzicht & Statistiek", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = dg.SelectedItems.Cast<GridRow>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Kies één of meerdere rijen om te verwijderen.", "Overzicht & Statistiek",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Verwijder {selected.Count} rij(en)?", "Bevestigen",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var setIds = selected.Select(r => r.SessionSetId).ToList();
            var sessIds = selected.Select(r => r.SessionId).Distinct().ToList();

            var sets = await db.SessionSets.Where(s => setIds.Contains(s.Id)).ToListAsync();
            db.SessionSets.RemoveRange(sets);

            var emptySessions = await db.Sessions
                .Where(s => sessIds.Contains(s.Id))
                .Where(s => !db.SessionSets.Any(ss => ss.SessionId == s.Id))
                .ToListAsync();
            if (emptySessions.Count > 0) db.Sessions.RemoveRange(emptySessions);

            await db.SaveChangesAsync();
            await LoadAsync();
        }

        private sealed class GridRow
        {
            public int SessionSetId { get; set; }
            public int SessionId { get; set; }
            public int ExerciseId { get; set; }
            public string ExerciseName { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
            public DateTime Date { get; set; }
            public bool IsPr { get; set; }
        }
    }
}
