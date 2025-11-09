// Export/Import van sessies naar/van JSON (platte DTO’s voor simpele uitwisseling).

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
        // Export: toon dialoog → haal sessies + sets op → serialize naar JSON-bestand.
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

            // Projecteer naar exportvriendelijke DTO’s (met oefeningnaam in plaats van Id).
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

            // Netjes geformatteerde JSON schrijven.
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(dlg.FileName, json);
        }

        // Import: kies JSON → lees DTO’s → zorg dat oefeningen bestaan → maak sessies + sets aan.
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
                // Nieuwe sessie per item in het JSON-bestand.
                var sess = new Session { Title = s.Title, Date = s.Date };
                db.Sessions.Add(sess);

                // Voor elke set: oefening opzoeken (of aanmaken) en set toevoegen.
                foreach (var set in s.Sets)
                {
                    var ex = await db.Exercises.FirstOrDefaultAsync(e => e.Name == set.ExerciseName);
                    if (ex == null)
                    {
                        ex = new Exercise { Name = set.ExerciseName, Category = "" };
                        db.Exercises.Add(ex);
                        await db.SaveChangesAsync(); // zodat ex.Id beschikbaar is
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
            await LoadAsync(); // grid verversen
        }

        // Simpele export-DTO voor een sessie.
        internal record SessionExportDto
        {
            public string Title { get; set; } = "";
            public DateTime Date { get; set; }
            public System.Collections.Generic.List<SetExportDto> Sets { get; set; } = new();
        }

        // Simpele export-DTO voor een set (met naam i.p.v. ExerciseId).
        internal record SetExportDto
        {
            public string ExerciseName { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
            public DateTime Date { get; set; }
        }
    }
}
