// Stat-berekening voor Sessions: periode kiezen, weekvolume, beste set per oefening.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.App.Helpers;
using WorkoutCoachV2.Model.Data;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class SessionsViewModel
    {
        // Lijst met “beste” sets per oefening (voor weergave).
        public ObservableCollection<BestSetItem> BestSets { get; } = new();

        // Filter: van-datum.
        private DateTime _fromDate;
        public DateTime FromDate
        {
            get => _fromDate;
            set => SetProperty(ref _fromDate, value);
        }

        // Filter: tot-datum.
        private DateTime _toDate;
        public DateTime ToDate
        {
            get => _toDate;
            set => SetProperty(ref _toDate, value);
        }

        // Volume (gewicht * reps) in de gekozen periode, afgerond naar int.
        private int _weekVolume;
        public int WeekVolume
        {
            get => _weekVolume;
            set => SetProperty(ref _weekVolume, value);
        }

        // Bereken stats: filter sessies, som volume, pick beste set per oefening.
        private async Task CalcStatsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Sla sets (met oefening en sessiedatum) plat voor aggregaties.
            var query = db.Sessions
                .AsNoTracking()
                .Include(s => s.Sets).ThenInclude(x => x.Exercise)
                .Where(s => s.Date >= FromDate && s.Date <= ToDate);

            var all = await query
                .SelectMany(s => s.Sets.Select(x => new
                {
                    SetId = x.Id,
                    ExerciseName = x.Exercise.Name,
                    Weight = x.Weight,
                    Reps = x.Reps,
                    Date = s.Date
                }))
                .ToListAsync();

            // Weekvolume = som(gewicht * reps).
            WeekVolume = (int)Math.Round(all.Sum(a => a.Weight * a.Reps));

            // Beste set per oefening: hoogste gewicht, dan reps, dan recentste datum.
            var best = all
                .GroupBy(a => a.ExerciseName)
                .Select(g => g.OrderByDescending(a => a.Weight)
                              .ThenByDescending(a => a.Reps)
                              .ThenByDescending(a => a.Date)
                              .First())
                .OrderBy(a => a.ExerciseName)
                .ToList();

            // UI-lijst vullen (markeer als PR in deze context).
            BestSets.Clear();
            foreach (var b in best)
            {
                BestSets.Add(new BestSetItem
                {
                    SetId = b.SetId,
                    ExerciseName = b.ExerciseName,
                    Weight = b.Weight,
                    Reps = b.Reps,
                    Date = b.Date,
                    IsPr = true
                });
            }
        }
    }

    // View-model item voor de statistiekentabel.
    public class BestSetItem : BaseViewModel
    {
        public int SetId { get; set; }
        public string ExerciseName { get; set; } = "";
        public double Weight { get; set; }
        public int Reps { get; set; }
        public DateTime Date { get; set; }

        private bool _isPr;
        public bool IsPr
        {
            get => _isPr;
            set => SetProperty(ref _isPr, value);
        }
    }
}
