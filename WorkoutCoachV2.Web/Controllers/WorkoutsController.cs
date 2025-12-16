using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize]
    public class WorkoutsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<WorkoutsController> _logger;

        public WorkoutsController(AppDbContext context, ILogger<WorkoutsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

      
        public async Task<IActionResult> Index(string? search, DateTime? from, DateTime? to, string? sort)
        {
            var userId = CurrentUserId;

            var query = _context.Workouts
                .Where(w => w.OwnerId == userId && !w.IsDeleted)
                .Include(w => w.Exercises)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(w => EF.Functions.Like(w.Title, pattern));
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(w => w.ScheduledOn.HasValue && w.ScheduledOn.Value >= fromDate);
            }

            if (to.HasValue)
            {
                var toExclusive = to.Value.Date.AddDays(1);
                query = query.Where(w => w.ScheduledOn.HasValue && w.ScheduledOn.Value < toExclusive);
            }

            sort = string.IsNullOrWhiteSpace(sort) ? "date_desc" : sort;

            query = sort switch
            {
                "date_asc" => query.OrderBy(w => w.ScheduledOn ?? DateTime.MaxValue).ThenBy(w => w.Title),
                "title_asc" => query.OrderBy(w => w.Title),
                "title_desc" => query.OrderByDescending(w => w.Title),
                _ => query.OrderByDescending(w => w.ScheduledOn ?? DateTime.MinValue).ThenBy(w => w.Title),
            };

            ViewData["Search"] = search ?? "";
            ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? "";
            ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? "";
            ViewData["Sort"] = sort;

            var workouts = await query.ToListAsync();
            return View(workouts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId && !w.IsDeleted);

            if (workout == null) return NotFound();

            var workoutExercises = await _context.WorkoutExercises
                .AsNoTracking()
                .Include(we => we.Exercise)
                .Where(we => we.WorkoutId == workout.Id)
                .OrderBy(we => we.Exercise != null ? we.Exercise.Name : "")
                .ToListAsync();

            ViewBag.WorkoutExercises = workoutExercises;

            return View(workout);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ScheduledOn")] Workout workout)
        {
            if (!ModelState.IsValid) return View(workout);

            workout.OwnerId = CurrentUserId;
            workout.CreatedAt = DateTime.UtcNow;
            workout.UpdatedAt = DateTime.UtcNow;
            workout.IsDeleted = false;

            _context.Workouts.Add(workout);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Workout aangemaakt. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Workout Create. OwnerId={OwnerId}", workout.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(workout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Workout Create. OwnerId={OwnerId}", workout.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(workout);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId && !w.IsDeleted);

            if (workout == null) return NotFound();

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ScheduledOn")] Workout formWorkout)
        {
            if (id != formWorkout.Id) return NotFound();
            if (!ModelState.IsValid) return View(formWorkout);

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId && !w.IsDeleted);

            if (workout == null) return NotFound();

            workout.Title = formWorkout.Title;
            workout.ScheduledOn = formWorkout.ScheduledOn;
            workout.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Workout aangepast. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Workout Edit. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(formWorkout);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Workout Edit. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(formWorkout);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId && !m.IsDeleted);

            if (workout == null) return NotFound();

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId && !w.IsDeleted);

            if (workout == null) return NotFound();

            try
            {
                workout.IsDeleted = true;
                workout.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Workout verwijderd (soft). WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Workout Delete. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                TempData["Error"] = "Er ging iets mis bij het verwijderen in de databank.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Workout Delete. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                TempData["Error"] = "Er ging iets mis bij het verwijderen.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Exercises(int id)
        {
            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .Include(w => w.Exercises)
                .ThenInclude(we => we.Exercise)
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId && !w.IsDeleted);

            if (workout == null) return NotFound();

            var allExercises = await _context.Exercises
                .Where(e => e.OwnerId == userId && !e.IsDeleted)
                .OrderBy(e => e.Name)
                .ToListAsync();

            var existing = workout.Exercises.ToDictionary(x => x.ExerciseId, x => x);

            var vm = new WorkoutExercisesEditViewModel
            {
                WorkoutId = workout.Id,
                WorkoutTitle = workout.Title,
                Items = allExercises.Select(e =>
                {
                    var isSelected = existing.ContainsKey(e.Id);
                    var reps = isSelected ? existing[e.Id].Reps : 0;
                    var weight = isSelected ? (existing[e.Id].WeightKg ?? 0) : 0;

                    return new WorkoutExerciseRowViewModel
                    {
                        ExerciseId = e.Id,
                        ExerciseName = e.Name,
                        IsSelected = isSelected,
                        Reps = reps,
                        WeightKg = weight
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Exercises(WorkoutExercisesEditViewModel model)
        {
            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .Include(w => w.Exercises)
                .FirstOrDefaultAsync(w => w.Id == model.WorkoutId && w.OwnerId == userId && !w.IsDeleted);

            if (workout == null) return NotFound();

            var allowedExerciseIds = await _context.Exercises
                .Where(e => e.OwnerId == userId && !e.IsDeleted)
                .Select(e => e.Id)
                .ToListAsync();

            var existing = await _context.WorkoutExercises
                .Where(we => we.WorkoutId == workout.Id)
                .ToListAsync();

            _context.WorkoutExercises.RemoveRange(existing);

            var selected = (model.Items ?? new List<WorkoutExerciseRowViewModel>())
                .Where(i => i.IsSelected && allowedExerciseIds.Contains(i.ExerciseId))
                .Select(i => new WorkoutExercise
                {
                    WorkoutId = workout.Id,
                    ExerciseId = i.ExerciseId,
                    Reps = i.Reps,
                    WeightKg = i.WeightKg
                })
                .ToList();

            if (selected.Count > 0)
                _context.WorkoutExercises.AddRange(selected);

            workout.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Workout oefeningen aangepast. WorkoutId={WorkoutId} OwnerId={OwnerId} Removed={RemovedCount} Added={AddedCount}",
                    workout.Id, workout.OwnerId, existing.Count, selected.Count);

                return RedirectToAction(nameof(Details), new { id = workout.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Workout Exercises POST. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                TempData["Error"] = "Er ging iets mis bij het opslaan in de databank.";
                return RedirectToAction(nameof(Exercises), new { id = workout.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Workout Exercises POST. WorkoutId={WorkoutId} OwnerId={OwnerId}", workout.Id, workout.OwnerId);
                TempData["Error"] = "Er ging iets mis bij het opslaan.";
                return RedirectToAction(nameof(Exercises), new { id = workout.Id });
            }
        }
    }
}
