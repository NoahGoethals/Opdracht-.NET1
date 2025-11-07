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
        public List<Exercise> Exercises { get; private set; } = new();

        private readonly ObservableCollection<Row> _rows = new();

        public StatsView()
        {
            InitializeComponent();
            Loaded += async (_, __) => await LoadAsync();
        }

        private static DateTime? ReadSessionDate(Session s)
        {
            var t = s.GetType();
            object? v =
                t.GetProperty("Date")?.GetValue(s) ??
                t.GetProperty("ScheduledOn")?.GetValue(s) ??
                t.GetProperty("ScheduledAt")?.GetValue(s) ??
                t.GetProperty("PerformedOn")?.GetValue(s) ??
                t.GetProperty("CreatedAt")?.GetValue(s);

            return v is DateTime dt ? dt : (DateTime?)null;
        }

        private static void WriteSessionDate(Session s, DateTime date)
        {
            var t = s.GetType();
            var p = t.GetProperty("Date")
                     ?? t.GetProperty("ScheduledOn")
                     ?? t.GetProperty("ScheduledAt")
                     ?? t.GetProperty("PerformedOn")
                     ?? t.GetProperty("CreatedAt");
            p?.SetValue(s, date);
        }

        private static bool ReadIsPr(SessionSet ss)
        {
            var p = ss.GetType().GetProperty("IsPr") ?? ss.GetType().GetProperty("Pr");
            var v = p?.GetValue(ss);
            return v is bool b && b;
        }

        private static void WriteIsPr(SessionSet ss, bool value)
        {
            var p = ss.GetType().GetProperty("IsPr") ?? ss.GetType().GetProperty("Pr");
            p?.SetValue(ss, value);
        }

        private async Task LoadAsync()
        {
            _rows.Clear();
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            Exercises = await db.Exercises.AsNoTracking()
                              .OrderBy(e => e.Name)
                              .ToListAsync();

            var sets = await db.SessionSets
                .Include(ss => ss.Session)
                .Include(ss => ss.Exercise)
                .AsNoTracking()
                .OrderByDescending(ss => ss.Session.Id)       
                .Take(200)
                .ToListAsync();

            foreach (var ss in sets)
            {
                _rows.Add(new Row
                {
                    Id = ss.Id,
                    ExerciseId = ss.ExerciseId,
                    Weight = ss.Weight,
                    Reps = ss.Reps,
                    Date = ReadSessionDate(ss.Session) ?? DateTime.Today,
                    IsPr = ReadIsPr(ss)
                });
            }

            dg.ItemsSource = _rows;
            DataContext = this;
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private void BtnNewRow_Click(object sender, RoutedEventArgs e)
        {
            var firstExId = Exercises.FirstOrDefault()?.Id ?? 0;
            _rows.Insert(0, new Row
            {
                Id = 0,
                ExerciseId = firstExId,
                Weight = 0,
                Reps = 5,
                Date = DateTime.Today,
                IsPr = false,
                IsNew = true
            });
            dg.SelectedIndex = 0;
            dg.ScrollIntoView(_rows[0]);
            dg.BeginEdit();
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            dg.CommitEdit(DataGridEditingUnit.Cell, true);
            dg.CommitEdit(DataGridEditingUnit.Row, true);

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var row in _rows.ToList())
            {
                if (row.ExerciseId <= 0) continue;

                if (row.Id == 0)
                {
                    var session = new Session
                    {
                        Title = "Log",
                    };
                    WriteSessionDate(session, row.Date.Date);

                    db.Sessions.Add(session);
                    await db.SaveChangesAsync(); 

                    var set = new SessionSet
                    {
                        SessionId = session.Id,
                        ExerciseId = row.ExerciseId,
                        Reps = row.Reps,
                        Weight = row.Weight
                    };
                    WriteIsPr(set, row.IsPr);

                    db.SessionSets.Add(set);
                    await db.SaveChangesAsync();

                    row.Id = set.Id;     
                    row.IsNew = false;
                }
                else
                {
                    var ss = await db.SessionSets
                        .Include(x => x.Session)
                        .FirstOrDefaultAsync(x => x.Id == row.Id);

                    if (ss is null) continue;

                    ss.ExerciseId = row.ExerciseId;
                    ss.Reps = row.Reps;
                    ss.Weight = row.Weight;
                    WriteIsPr(ss, row.IsPr);

                    if (ss.Session is not null)
                        WriteSessionDate(ss.Session, row.Date.Date);

                    await db.SaveChangesAsync();
                }
            }

            MessageBox.Show("Opgeslagen.", "Overzicht & Statistiek", MessageBoxButton.OK, MessageBoxImage.Information);
            await LoadAsync();
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selected = dg.SelectedItems.Cast<Row>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Selecteer één of meerdere rijen.", "Verwijderen",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"Verwijder {selected.Count} rij(en)?", "Bevestigen",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var r in selected)
            {
                if (r.Id == 0)
                {
                    _rows.Remove(r);
                    continue;
                }

                var ss = await db.SessionSets.FirstOrDefaultAsync(x => x.Id == r.Id);
                if (ss != null) db.SessionSets.Remove(ss);
            }

            await db.SaveChangesAsync();
            await LoadAsync();
        }

        private sealed class Row
        {
            public int Id { get; set; }                
            public int ExerciseId { get; set; }
            public double Weight { get; set; }
            public int Reps { get; set; }
            public DateTime Date { get; set; }
            public bool IsPr { get; set; }
            public bool IsNew { get; set; }             
        }
    }
}
