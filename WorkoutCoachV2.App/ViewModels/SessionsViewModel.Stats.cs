using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.Model.Data;

namespace WorkoutCoachV2.App.ViewModels
{
    public partial class SessionsViewModel
    {
        private DateTime _dateFrom;
        public DateTime DateFrom { get => _dateFrom; set => SetProperty(ref _dateFrom, value); }

        private DateTime _dateTo;
        public DateTime DateTo { get => _dateTo; set => SetProperty(ref _dateTo, value); }

        private double _weekVolume;
        public double WeekVolume { get => _weekVolume; set => SetProperty(ref _weekVolume, value); }

        public ObservableCollection<BestSetItem> BestSets { get; } = new();

        public async Task LoadStatsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var all = await db.SessionSets
                .Where(x => !x.IsDeleted
                            && !x.Session.IsDeleted
                            && x.Exercise != null
                            && !x.Exercise!.IsDeleted)
                .Select(x => new RawSet
                {
                    ExerciseName = x.Exercise!.Name,
                    Reps = x.Reps,
                    Weight = x.Weight,
                    Date = x.Session.Date
                })
                .ToListAsync();

            var from = DateFrom.Date;
            var to = DateTo.Date.AddDays(1).AddTicks(-1);

            var period = all.Where(s => s.Date >= from && s.Date <= to).ToList();

            WeekVolume = Math.Round(period.Sum(s => s.Weight * s.Reps), 2);

            var periodBest = period
                .GroupBy(s => s.ExerciseName)
                .Select(g => g.OrderByDescending(x => x.Weight)
                              .ThenByDescending(x => x.Reps)
                              .First())
                .ToList();

            BestSets.Clear();
            foreach (var bp in periodBest)
            {
                var bestBefore = all
                    .Where(s => s.ExerciseName == bp.ExerciseName && s.Date < from)
                    .OrderByDescending(x => x.Weight)
                    .ThenByDescending(x => x.Reps)
                    .FirstOrDefault();

                bool isNewPr = bestBefore == null
                               || bp.Weight > bestBefore.Weight
                               || (Math.Abs(bp.Weight - bestBefore.Weight) < 0.0001 && bp.Reps > bestBefore.Reps);

                BestSets.Add(new BestSetItem
                {
                    ExerciseName = bp.ExerciseName,
                    Weight = bp.Weight,
                    Reps = bp.Reps,
                    Date = bp.Date,
                    IsNewPr = isNewPr
                });
            }
        }

        private class RawSet
        {
            public string ExerciseName { get; set; } = "";
            public int Reps { get; set; }
            public double Weight { get; set; }
            public DateTime Date { get; set; }
        }

        public class BestSetItem
        {
            public string ExerciseName { get; set; } = "";
            public double Weight { get; set; }
            public int Reps { get; set; }
            public DateTime Date { get; set; }
            public bool IsNewPr { get; set; }
            public double Volume => Weight * Reps;
        }
    }
}
