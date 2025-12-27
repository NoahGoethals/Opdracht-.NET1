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
    // Toont statistieken op basis van SessionSets (met filters: oefening + datumrange) en laadt resultaten ook via Ajax.
    [Authorize]
    public class StatsController : Controller
    {
        private readonly AppDbContext _context; // Leest sets/sessies/oefeningen uit de databank.
        private readonly ILogger<StatsController> _logger; // Logt fouten tijdens statistiek-berekening.
        private readonly IStringLocalizer<SharedResource> _localizer; // Voor vertaalde UI-teksten (bv. "Alle oefeningen").

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
            User.FindFirstValue(ClaimTypes.NameIdentifier); // UserId uit claims, kan null zijn.

        [HttpGet]
        public async Task<IActionResult> Index(int? exerciseId, DateTime? from, DateTime? to)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
                return Challenge(); // Niet ingelogd => naar login.

            var vm = new StatsIndexViewModel
            {
                ExerciseId = exerciseId, // Huidige filter: oefening.
                From = from, // Huidige filter: van.
                To = to, // Huidige filter: tot.
                Exercises = await BuildExerciseSelectListAsync(exerciseId) // Dropdown met oefeningen.
            };

            try
            {
                vm.Results = await BuildResultsAsync(exerciseId, from, to); // Berekent alle statistieken.
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
                }; // Lege resultaten zodat de pagina niet crasht.

                TempData["Error"] = "Er ging iets mis bij het laden van de statistieken.";
            }

            return View(vm); // Volledige stats pagina.
        }

        [HttpGet]
        public async Task<IActionResult> Results(int? exerciseId, DateTime? from, DateTime? to)
        {
            if (string.IsNullOrWhiteSpace(CurrentUserId))
                return Challenge(); // Niet ingelogd => geen Ajax data.

            try
            {
                var results = await BuildResultsAsync(exerciseId, from, to); // Zelfde berekening, maar enkel partial.
                return PartialView("_StatsResults", results); // Wordt gebruikt door Ajax refresh.
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
                }; // Fallback partial.

                Response.StatusCode = 500; // Ajax kan zien dat dit mislukte.
                return PartialView("_StatsResults", empty);
            }
        }

        private async Task<SelectListItem[]> BuildExerciseSelectListAsync(int? selectedId)
        {
            var userId = CurrentUserId!; // Hier is userId gegarandeerd.

            var items = await _context.Exercises
                .AsNoTracking()
                .Where(e => !e.IsDeleted && e.OwnerId == userId)
                .OrderBy(e => e.Name)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(), // Value = exerciseId.
                    Text = e.Name, // Tekst in dropdown.
                    Selected = selectedId.HasValue && e.Id == selectedId.Value // Huidige selectie.
                })
                .ToListAsync();

            items.Insert(0, new SelectListItem
            {
                Value = "",
                Text = _localizer["St_AllExercises"], // "Alle oefeningen" (vertaald).
                Selected = !selectedId.HasValue
            }); // Extra optie om geen oefening te filteren.

            return items.ToArray(); // Wordt gebruikt in de view.
        }

        private async Task<StatsResultsViewModel> BuildResultsAsync(int? exerciseId, DateTime? from, DateTime? to)
        {
            var userId = CurrentUserId!; // Hier is userId gegarandeerd.

            var q = _context.SessionSets
                .AsNoTracking()
                .Include(ss => ss.Session)
                .Include(ss => ss.Exercise)
                .AsQueryable(); // Start query op alle sets met session+exercise.

            q = q.Where(ss =>
                !ss.IsDeleted
                && ss.Session != null
                && !ss.Session.IsDeleted
                && ss.Session.OwnerId == userId
                && ss.Exercise != null
                && !ss.Exercise.IsDeleted
                && ss.Exercise.OwnerId == userId); // Security: enkel eigen data + geen soft-deletes.

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                q = q.Where(ss => ss.Session.Date >= fromDate); // Vanaf datum.
            }

            if (to.HasValue)
            {
                var toDate = to.Value.Date;
                q = q.Where(ss => ss.Session.Date <= toDate); // Tot datum.
            }

            if (exerciseId.HasValue)
            {
                q = q.Where(ss => ss.ExerciseId == exerciseId.Value); // Filter op 1 oefening.
            }

            var sets = await q.ToListAsync(); // Haalt sets op en berekent daarna in C#.

            var result = new StatsResultsViewModel
            {
                ExerciseId = exerciseId,
                From = from,
                To = to
            }; // Basismodel voor de view.

            if (exerciseId.HasValue)
            {
                var first = sets.FirstOrDefault();
                result.ExerciseName = first?.Exercise?.Name; // Naam van de oefening voor titel.

                result.SetsCount = sets.Count; // Aantal sets.
                result.SessionsCount = sets.Select(s => s.SessionId).Distinct().Count(); // Unieke sessies.
                result.TotalReps = sets.Sum(s => s.Reps); // Som reps.
                result.TotalVolumeKg = sets.Sum(s => s.Weight * s.Reps); // Totaal volume (kg * reps).

                if (sets.Count > 0)
                {
                    result.MaxWeight = sets.Max(s => s.Weight); // Zwaarste gewicht.
                    result.BestEstimated1Rm = sets.Max(s => s.Weight * (1.0 + (s.Reps / 30.0))); // Eenvoudige 1RM schatting.
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
                    .ToList(); // Breakdown per sessie.
            }
            else
            {
                result.SetsCount = sets.Count; // Alles samen.
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
                        TotalVolumeKg = g.Sum(x => x.Weight * x.Reps), // Volume per oefening.
                        MaxWeight = g.Max(x => x.Weight) // Max gewicht per oefening.
                    })
                    .OrderByDescending(x => x.TotalVolumeKg)
                    .Take(10)
                    .ToList(); // Top 10 oefeningen op volume.
            }

            return result; // Wordt gerenderd in _StatsResults.
        }
    }
}
