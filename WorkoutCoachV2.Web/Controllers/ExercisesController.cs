using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize]
    public class ExercisesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExercisesController> _logger;

        public ExercisesController(AppDbContext context, ILogger<ExercisesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;

            var exercises = await _context.Exercises
                .Where(e => e.OwnerId == userId && !e.IsDeleted)
                .OrderBy(e => e.Name)
                .ToListAsync();

            return View(exercises);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId && !m.IsDeleted);

            if (exercise == null) return NotFound();

            return View(exercise);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Category,Notes")] Exercise exercise)
        {
            if (!ModelState.IsValid) return View(exercise);

            exercise.OwnerId = CurrentUserId;
            exercise.CreatedAt = DateTime.UtcNow;
            exercise.UpdatedAt = DateTime.UtcNow;
            exercise.IsDeleted = false;

            _context.Exercises.Add(exercise);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Exercise aangemaakt. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Exercise Create. OwnerId={OwnerId}", exercise.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(exercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Exercise Create. OwnerId={OwnerId}", exercise.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(exercise);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId && !e.IsDeleted);

            if (exercise == null) return NotFound();

            return View(exercise);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,Notes")] Exercise formExercise)
        {
            if (id != formExercise.Id) return NotFound();
            if (!ModelState.IsValid) return View(formExercise);

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId && !e.IsDeleted);

            if (exercise == null) return NotFound();

            exercise.Name = formExercise.Name;
            exercise.Category = formExercise.Category;
            exercise.Notes = formExercise.Notes;
            exercise.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Exercise aangepast. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Exercise Edit. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(formExercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Exercise Edit. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(formExercise);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId && !m.IsDeleted);

            if (exercise == null) return NotFound();

            return View(exercise);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId && !e.IsDeleted);

            if (exercise == null) return NotFound();

            try
            {
                var workoutLinks = await _context.WorkoutExercises
                    .Where(we => we.ExerciseId == id)
                    .ToListAsync();

                if (workoutLinks.Count > 0)
                    _context.WorkoutExercises.RemoveRange(workoutLinks);

                var sessionSets = await _context.SessionSets
                    .Where(ss => ss.ExerciseId == id)
                    .ToListAsync();

                if (sessionSets.Count > 0)
                    _context.SessionSets.RemoveRange(sessionSets);

                exercise.IsDeleted = true;
                exercise.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Exercise verwijderd (soft). ExerciseId={ExerciseId} OwnerId={OwnerId} RemovedWorkoutLinks={WorkoutLinks} RemovedSessionSets={SessionSets}",
                    exercise.Id, exercise.OwnerId, workoutLinks.Count, sessionSets.Count);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Exercise Delete. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                TempData["Error"] = "Er ging iets mis bij het verwijderen in de databank.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Exercise Delete. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                TempData["Error"] = "Er ging iets mis bij het verwijderen.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }
    }
}
