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

namespace WorkoutCoachV2.Web.Controllers
{
    // UI controller voor oefeningen: lijst met filters/sort + CRUD met soft delete.
    [Authorize] // Alleen ingelogde gebruikers.
    public class ExercisesController : Controller
    {
        private readonly AppDbContext _context; // Databank via EF Core.
        private readonly ILogger<ExercisesController> _logger; // Logging voor create/edit/delete.

        public ExercisesController(AppDbContext context, ILogger<ExercisesController> logger)
        {
            _context = context; // DI DbContext.
            _logger = logger; // DI logger.
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; // OwnerId uit cookie/claims.

        public async Task<IActionResult> Index(string? search, string? category, string? sort)
        {
            var userId = CurrentUserId; // Alles is per gebruiker.

            var query = _context.Exercises
                .Where(e => e.OwnerId == userId && !e.IsDeleted)
                .AsQueryable(); // Basis: enkel eigen oefeningen, niet verwijderd.

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(e => EF.Functions.Like(e.Name, pattern)); // Zoeken op naam (SQL LIKE).
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(e => e.Category == category); // Filter op categorie.
            }

            sort = string.IsNullOrWhiteSpace(sort) ? "name_asc" : sort; // Default sort.

            query = sort switch
            {
                "name_desc" => query.OrderByDescending(e => e.Name),
                "cat_asc" => query.OrderBy(e => e.Category).ThenBy(e => e.Name),
                "cat_desc" => query.OrderByDescending(e => e.Category).ThenBy(e => e.Name),
                _ => query.OrderBy(e => e.Name),
            }; // Sorteert lijst.

            var categories = await _context.Exercises
                .Where(e => e.OwnerId == userId && !e.IsDeleted && e.Category != null && e.Category != "")
                .Select(e => e.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync(); // Bouwt dropdown met beschikbare categorieën.

            ViewData["Search"] = search ?? ""; // Houdt filterwaarden vast in UI.
            ViewData["Category"] = category ?? "";
            ViewData["Sort"] = sort;
            ViewBag.CategoryOptions = categories; // Dropdown opties.

            var exercises = await query.ToListAsync(); // Haalt lijst op.
            return View(exercises); // Toont index view.
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound(); // Geen id => 404.

            var userId = CurrentUserId; // Ownership check.

            var exercise = await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId && !m.IsDeleted); // Enkel eigen item.

            if (exercise == null) return NotFound(); // Niet gevonden/geen rechten.

            return View(exercise); // Toont details.
        }

        public IActionResult Create()
        {
            return View(); // Toont create formulier.
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Category,Notes")] Exercise exercise)
        {
            if (!ModelState.IsValid) return View(exercise); // Validatie via data annotations.

            exercise.OwnerId = CurrentUserId; // Koppelt oefening aan ingelogde user.
            exercise.CreatedAt = DateTime.UtcNow; // Timestamps.
            exercise.UpdatedAt = DateTime.UtcNow;
            exercise.IsDeleted = false; // Soft delete flag.

            _context.Exercises.Add(exercise); // Insert.

            try
            {
                await _context.SaveChangesAsync(); // Opslaan.
                _logger.LogInformation("Exercise aangemaakt. ExerciseId={ExerciseId} OwnerId={OwnerId}", exercise.Id, exercise.OwnerId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Exercise Create. OwnerId={OwnerId}", exercise.OwnerId); // DB fout loggen.
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(exercise);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Exercise Create. OwnerId={OwnerId}", exercise.OwnerId); // Algemene fout loggen.
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(exercise);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound(); // Geen id => 404.

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId && !e.IsDeleted); // Enkel eigen item.

            if (exercise == null) return NotFound();

            return View(exercise); // Toont edit formulier.
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,Notes")] Exercise formExercise)
        {
            if (id != formExercise.Id) return NotFound(); // Anti-tamper check.
            if (!ModelState.IsValid) return View(formExercise); // Validatie.

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId && !e.IsDeleted); // Echte entity ophalen.

            if (exercise == null) return NotFound();

            exercise.Name = formExercise.Name; // Velden updaten.
            exercise.Category = formExercise.Category;
            exercise.Notes = formExercise.Notes;
            exercise.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync(); // Opslaan.
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
            if (id == null) return NotFound(); // Geen id => 404.

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId && !m.IsDeleted); // Enkel eigen item.

            if (exercise == null) return NotFound();

            return View(exercise); // Toont delete confirm page.
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId && !e.IsDeleted); // Enkel eigen item.

            if (exercise == null) return NotFound();

            try
            {
                var workoutLinks = await _context.WorkoutExercises
                    .Where(we => we.ExerciseId == id)
                    .ToListAsync(); // Koppelingen met workouts opruimen.

                if (workoutLinks.Count > 0)
                    _context.WorkoutExercises.RemoveRange(workoutLinks);

                var sessionSets = await _context.SessionSets
                    .Where(ss => ss.ExerciseId == id)
                    .ToListAsync(); // Sets in sessies opruimen.

                if (sessionSets.Count > 0)
                    _context.SessionSets.RemoveRange(sessionSets);

                exercise.IsDeleted = true; // Soft delete i.p.v. echt verwijderen.
                exercise.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync(); // Alles in 1 save.

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
