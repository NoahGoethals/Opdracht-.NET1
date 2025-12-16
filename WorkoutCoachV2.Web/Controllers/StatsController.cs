using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize]
    public class StatsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StatsController> _logger;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public StatsController(
            AppDbContext context,
            ILogger<StatsController> logger,
            IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _logger = logger;
            _localizer = localizer;
        }

        private string? CurrentUserId =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Index(int? exerciseId, DateTime? from, DateTime? to)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
                return Challenge();

            var vm = new StatsIndexViewModel
            {
                ExerciseId = exerciseId,
                From = from,
                To = to,
                Exercises = await BuildExerciseSelectListAsync(exerciseId)
            };

            try
            {
                vm.Results = await BuildResultsAsync(exerciseId, from, to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Stats Index: fout bij BuildResultsAsync. UserId={UserId} ExerciseId={ExerciseId} From={From} To={To}",
                    CurrentUserId, exerciseId, from, to);

                vm.Results = new StatsResultsViewModel
                {
                    ExerciseId = exerciseId,
                    From = from,
                    To = to
                };

                TempData["Error"] = "Er ging iets mis bij het laden van de statistieken.";
            }

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> Results(int? exerciseId, DateTime? from, DateTime? to)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
                return Challenge();

            try
            {
                var results = await BuildResultsAsync(exerciseId, from, to);
                return PartialView("_StatsResults", results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Stats Results: fout bij BuildResultsAsync. UserId={UserId} ExerciseId={ExerciseId} From={From} To={To}",
                    CurrentUserId, exerciseId, from, to);

                var empty = new StatsResultsViewModel
                {
                    ExerciseId = exerciseId,
                    From = from,
                    To = to
                };

                Response.StatusCode = 500;
                return PartialView("_StatsResults", empty);
            }
        }

        private async Task<SelectListItem[]> BuildExerciseSelectListAsync(int? selectedId)
        {
            var userId = CurrentUserId!;

            var items = await _context.Exercises
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.OwnerId == userId)
                .OrderBy(e => e.Name)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = e.Name,
                    Selected = selectedId.HasValue && e.Id == selectedId.Value
                })
                .ToListAsync();

            items.Insert(0, new SelectListItem
            {
                Value = "",
                Text = _localizer["St_AllExercises"], 
                Selected = !selectedId.HasValue
            });

            return items.ToArray();
        }

        private async Task<StatsResultsViewModel> BuildResultsAsync(int? exerciseId, DateTime? from, DateTime? to)
        {
            var userId = CurrentUserId!;

            var q = _context.SessionSets
                .AsNoTracking()
                .Include(ss => ss.Session)
                .Include(ss => ss.Exercise)
                .AsQueryable();

            q = q.Where(ss =>
                !ss.IsDeleted
                && ss.Session != null
                && !ss.Session.IsDeleted
                && ss.Session.OwnerId == userId
                && ss.Exercise != null
                && !ss.Exercise.IsDeleted
                && ss.Exercise.OwnerId == userId);

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                q = q.Where(ss => ss.Session.Date >= fromDate);
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date;
                q = q.Where(ss => ss.Session.Date <= toDate);
            }

            if (exerciseId.HasValue)
            {
                q = q.Where(ss => ss.ExerciseId == exerciseId.Value);
            }

            var sets = await q.ToListAsync();

            var result = new StatsResultsViewModel
            {
                ExerciseId = exerciseId,
                From = from,
                To = to
            };

            if (exerciseId.HasValue)
            {
                var first = sets.FirstOrDefault();
                result.ExerciseName = first?.Exercise?.Name;

                result.SetsCount = sets.Count;
                result.SessionsCount = sets.Select(s => s.SessionId).Distinct().Count();
                result.TotalReps = sets.Sum(s => s.Reps);
                result.TotalVolumeKg = sets.Sum(s => s.Weight * s.Reps);

                if (sets.Count > 0)
                {
                    result.MaxWeight = sets.Max(s => s.Weight);
                    result.BestEstimated1Rm = sets.Max(s => s.Weight * (1.0 + (s.Reps / 30.0)));
                }

                result.PerSession = sets
                    .GroupBy(s => new { s.SessionId, s.Session.Date, s.Session.Title })
                    .OrderByDescending(g => g.Key.Date)
                    .Select(g => new StatsSessionRowViewModel
                    {
                        Date = g.Key.Date,
                        SessionTitle = g.Key.Title,
                        SetsCount = g.Count(),
                        TotalReps = g.Sum(x => x.Reps),
                        TotalVolumeKg = g.Sum(x => x.Weight * x.Reps),
                        MaxWeight = g.Max(x => x.Weight)
                    })
                    .ToList();
            }
            else
            {
                result.SetsCount = sets.Count;
                result.SessionsCount = sets.Select(s => s.SessionId).Distinct().Count();
                result.TotalReps = sets.Sum(s => s.Reps);
                result.TotalVolumeKg = sets.Sum(s => s.Weight * s.Reps);

                result.TopExercises = sets
                    .GroupBy(s => new { s.ExerciseId, s.Exercise.Name })
                    .Select(g => new StatsTopExerciseRowViewModel
                    {
                        ExerciseId = g.Key.ExerciseId,
                        ExerciseName = g.Key.Name,
                        SetsCount = g.Count(),
                        TotalReps = g.Sum(x => x.Reps),
                        TotalVolumeKg = g.Sum(x => x.Weight * x.Reps), // ✅ volume
                        MaxWeight = g.Max(x => x.Weight)               // ✅ max
                    })
                    .OrderByDescending(x => x.TotalVolumeKg)
                    .Take(10)
                    .ToList();
            }

            return result;
        }
    }
}
