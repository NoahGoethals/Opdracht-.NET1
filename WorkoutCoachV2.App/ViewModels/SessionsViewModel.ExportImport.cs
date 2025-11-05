using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class SessionsViewModel
    {
        private async Task ExportSessionsAsync()
        {
            var dlg = new SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                FileName = $"sessions_{DateTime.Now:yyyyMMdd}.json"
            };
            if (dlg.ShowDialog() != true) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var payload = await db.Sessions
                .AsNoTracking()
                .Include(s => s.Sets).ThenInclude(x => x.Exercise)
                .OrderBy(s => s.Date)
                .Select(s => new SessionExportDto
                {
                    Title = s.Title,
                    Date = s.Date,
                    Sets = s.Sets.Select(x => new SetExportDto
                    {
                        ExerciseName = x.Exercise.Name,
                        Weight = x.Weight,
                        Reps = x.Reps,
                        Date = s.Date
                    }).ToList()
                })
                .ToListAsync();

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(dlg.FileName, json);
        }

        private async Task ImportSessionsAsync()
        {
            var dlg = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
            if (dlg.ShowDialog() != true) return;

            var json = await File.ReadAllTextAsync(dlg.FileName);
            var payload = JsonSerializer.Deserialize<SessionExportDto[]>(json) ?? Array.Empty<SessionExportDto>();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            foreach (var s in payload)
            {
                var sess = new Session { Title = s.Title, Date = s.Date };
                db.Sessions.Add(sess);

                foreach (var set in s.Sets)
                {
                    var ex = await db.Exercises.FirstOrDefaultAsync(e => e.Name == set.ExerciseName);
                    if (ex == null)
                    {
                        ex = new Exercise { Name = set.ExerciseName, Category = "" };
                        db.Exercises.Add(ex);
                        await db.SaveChangesAsync();
                    }

                    sess.Sets.Add(new SessionSet
                    {
                        ExerciseId = ex.Id,
                        Weight = set.Weight,
                        Reps = set.Reps
                    });
                }
            }

            await db.SaveChangesAsync();
            await LoadAsync();
        }

        internal record SessionExportDto
        {
            public string Title { get; set; } = "";
            public DateTime Date { get; set; }
            public System.Collections.Generic.List<SetExportDto> Sets { get; set; } = new();
        }

        internal record SetExportDto
        {
            public string ExerciseName { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
            public DateTime Date { get; set; }
        }
    }
}
