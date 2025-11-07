using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkoutCoachV2.Model.Data;

namespace WorkoutCoachV2.App.View
{
    public partial class SessionDetailsWindow : Window
    {
        private readonly int _sessionId;

        public SessionDetailsWindow(int sessionId)
        {
            InitializeComponent();
            _sessionId = sessionId;
            Loaded += async (_, __) => await LoadAsync();
        }

        private static string ReadTitle(object s)
        {
            var t = s.GetType();
            return t.GetProperty("Title")?.GetValue(s)?.ToString()
                ?? t.GetProperty("Name")?.GetValue(s)?.ToString()
                ?? string.Empty;
        }

        private static DateTime? ReadDate(object s)
        {
            var t = s.GetType();
            object v = t.GetProperty("Date")?.GetValue(s)
                ?? t.GetProperty("ScheduledOn")?.GetValue(s)
                ?? t.GetProperty("ScheduledAt")?.GetValue(s)
                ?? t.GetProperty("PerformedOn")?.GetValue(s)
                ?? t.GetProperty("CreatedAt")?.GetValue(s)
                ?? t.GetProperty("UpdatedAt")?.GetValue(s);

            return v is DateTime dt ? dt : (DateTime?)null;
        }

        private static string ReadDescription(object s)
        {
            var t = s.GetType();
            return t.GetProperty("Description")?.GetValue(s)?.ToString()
                ?? t.GetProperty("Notes")?.GetValue(s)?.ToString()
                ?? t.GetProperty("Note")?.GetValue(s)?.ToString()
                ?? t.GetProperty("Comment")?.GetValue(s)?.ToString()
                ?? string.Empty;
        }

        private async Task LoadAsync()
        {
            using var scope = App.HostApp.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var session = await db.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => EF.Property<int>(s, "Id") == _sessionId);

            if (session is null)
            {
                MessageBox.Show("Sessie niet gevonden.", "Inhoud",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            tbTitle.Text = ReadTitle(session);
            var dt = ReadDate(session);
            tbDate.Text = dt?.ToString("yyyy-MM-dd") ?? string.Empty;

            var desc = ReadDescription(session);
            tbDescription.Text = string.IsNullOrWhiteSpace(desc) ? "(geen beschrijving)" : desc;

            var sets = await db.SessionSets
                .AsNoTracking()
                .Where(ss => ss.SessionId == _sessionId)
                .Select(ss => new SetRow
                {
                    Exercise = ss.Exercise != null
                        ? ss.Exercise.Name
                        : (ss.ExerciseId != 0 ? $"#{ss.ExerciseId}" : "-"),
                    Weight = ss.Weight,
                    Reps = ss.Reps
                })
                .ToListAsync();

            dgSets.ItemsSource = sets;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private sealed class SetRow
        {
            public string Exercise { get; set; } = "-";
            public double Weight { get; set; }
            public int Reps { get; set; }
        }
    }
}
