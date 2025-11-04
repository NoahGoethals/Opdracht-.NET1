using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.Services
{
    public class ExportImportService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ExportImportService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        }


        public async Task<int> ExportExercisesAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("Pad is leeg", nameof(filePath));

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var items = await db.Exercises
                .AsNoTracking()
                .Where(e => !e.IsDeleted)
                .Select(e => new ExerciseDto
                {
                    Name = e.Name,
                    Category = e.Category
                })
                .OrderBy(e => e.Name)
                .ToListAsync();

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            switch (ext)
            {
                case ".json":
                    var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
                    return items.Count;

                case ".csv":
                    var csv = ToCsv(items);
                    await File.WriteAllTextAsync(filePath, csv, Encoding.UTF8);
                    return items.Count;

                default:
                    throw new NotSupportedException($"Gebruik .json of .csv (niet: {ext})");
            }
        }

        public async Task<int> ImportExercisesAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Pad is leeg", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Bestand niet gevonden", filePath);

            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            List<ExerciseDto> items;

            switch (ext)
            {
                case ".json":
                    var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    items = JsonSerializer.Deserialize<List<ExerciseDto>>(json) ?? new();
                    break;

                case ".csv":
                    var csv = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    items = FromCsv(csv);
                    break;

                default:
                    throw new NotSupportedException($"Gebruik .json of .csv (niet: {ext})");
            }

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int created = 0;

            foreach (var dto in items)
            {
                if (string.IsNullOrWhiteSpace(dto.Name)) continue;

                var name = dto.Name.Trim();
                var existing = await db.Exercises.FirstOrDefaultAsync(e => e.Name == name);
                if (existing is null)
                {
                    db.Exercises.Add(new Exercise
                    {
                        Name = name,
                        Category = dto.Category?.Trim(),
                        IsDeleted = false
                    });
                    created++;
                }
                else
                {
                    if (existing.IsDeleted) existing.IsDeleted = false;
                    existing.Category = dto.Category?.Trim();
                }
            }

            await db.SaveChangesAsync();
            return created;
        }

        private static string ToCsv(IEnumerable<ExerciseDto> items)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Name;Category");
            foreach (var e in items)
            {
                sb.AppendLine(string.Join(";", Clean(e.Name), Clean(e.Category)));
            }
            return sb.ToString();

            static string Clean(string? s) =>
                s is null ? "" : s.Replace(";", ",").Replace("\r", " ").Replace("\n", " ");
        }

        private static List<ExerciseDto> FromCsv(string csv)
        {
            var lines = (csv ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<ExerciseDto>();
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (i == 0 && line.StartsWith("Name", StringComparison.OrdinalIgnoreCase)) continue;
                var parts = line.Split(';');
                list.Add(new ExerciseDto
                {
                    Name = parts.ElementAtOrDefault(0)?.Trim() ?? "",
                    Category = parts.ElementAtOrDefault(1)?.Trim()
                });
            }
            return list;
        }


        public async Task<int> ExportSessionsAsync(string filePath, DateTime? from = null, DateTime? to = null)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Pad is leeg", nameof(filePath));
            if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Sessions export: alleen .json");

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var q = db.Sessions.AsNoTracking().Where(s => !s.IsDeleted);
            if (from is not null) q = q.Where(s => s.Date >= from.Value);
            if (to is not null) q = q.Where(s => s.Date <= to.Value);

            var items = await q
                .OrderByDescending(s => s.Date)
                .Select(s => new SessionDto
                {
                    Title = s.Title,
                    Date = s.Date,
                    Sets = s.Sets
                        .Where(x => !x.IsDeleted && x.Exercise != null && !x.Exercise.IsDeleted)
                        .Select(x => new SessionSetDto
                        {
                            ExerciseName = x.Exercise!.Name,
                            Reps = x.Reps,
                            Weight = x.Weight,
                            Rpe = x.Rpe
                        }).ToList()
                })
                .ToListAsync();

            var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
            return items.Count;
        }

        public async Task<int> ImportSessionsAsync(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("Pad is leeg", nameof(filePath));
            if (!File.Exists(filePath)) throw new FileNotFoundException("Bestand niet gevonden", filePath);
            if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new NotSupportedException("Sessions import: alleen .json");

            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            var items = JsonSerializer.Deserialize<List<SessionDto>>(json) ?? new();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            int created = 0;

            foreach (var s in items)
            {
                if (s.Date == default) continue;
                var title = string.IsNullOrWhiteSpace(s.Title) ? $"Session {s.Date:yyyy-MM-dd}" : s.Title.Trim();

                var existing = await db.Sessions
                    .FirstOrDefaultAsync(x => x.Date.Date == s.Date.Date && x.Title == title);

                if (existing is null)
                {
                    existing = new Session
                    {
                        Title = title,
                        Date = s.Date,
                        IsDeleted = false
                    };
                    db.Sessions.Add(existing);
                    created++;
                }
                else
                {
                    if (existing.IsDeleted) existing.IsDeleted = false;
                }

                foreach (var set in s.Sets ?? new())
                {
                    if (string.IsNullOrWhiteSpace(set.ExerciseName)) continue;

                    var exName = set.ExerciseName.Trim();
                    var ex = await db.Exercises.FirstOrDefaultAsync(e => e.Name == exName);
                    if (ex is null)
                    {
                        ex = new Exercise { Name = exName, IsDeleted = false };
                        db.Exercises.Add(ex);
                        await db.SaveChangesAsync();
                    }

                    bool exists = await db.SessionSets.AnyAsync(x =>
                        x.SessionId == existing.Id &&
                        x.ExerciseId == ex.Id &&
                        x.Reps == set.Reps &&
                        Math.Abs(x.Weight - set.Weight) < 0.0001);

                    if (!exists)
                    {
                        db.SessionSets.Add(new SessionSet
                        {
                            Session = existing,
                            Exercise = ex,
                            Reps = set.Reps,
                            Weight = set.Weight,
                            Rpe = set.Rpe,
                            IsDeleted = false
                        });
                    }
                }
            }

            await db.SaveChangesAsync();
            return created;
        }


        public record ExerciseDto
        {
            public string? Name { get; init; }
            public string? Category { get; init; }
        }

        public record SessionDto
        {
            public string? Title { get; init; }
            public DateTime Date { get; init; }
            public List<SessionSetDto>? Sets { get; init; }
        }

        public record SessionSetDto
        {
            public string? ExerciseName { get; init; }
            public int Reps { get; init; }
            public double Weight { get; init; }
            public double? Rpe { get; init; }
        }
    }
}
