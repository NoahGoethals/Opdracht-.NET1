using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize]
    public class ExercisesController : Controller
    {
        private readonly AppDbContext _context;

        public ExercisesController(AppDbContext context)
        {
            _context = context;
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
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId);

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
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId);

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
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId);

            if (exercise == null) return NotFound();

            exercise.Name = formExercise.Name;
            exercise.Category = formExercise.Category;
            exercise.Notes = formExercise.Notes;
            exercise.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId);

            if (exercise == null) return NotFound();

            return View(exercise);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id && e.OwnerId == userId);

            if (exercise == null) return NotFound();

            var workoutLinks = await _context.WorkoutExercises
                .Where(we => we.ExerciseId == id)
                .ToListAsync();

            if (workoutLinks.Count > 0)
            {
                _context.WorkoutExercises.RemoveRange(workoutLinks);
            }

       
            var usedInSessions = await _context.SessionSets
                .AnyAsync(s => s.ExerciseId == id);

            if (usedInSessions)
            {
                exercise.IsDeleted = true;
                exercise.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Oefening is verwijderd (gearchiveerd) omdat ze gebruikt werd in sessies.";
                return RedirectToAction(nameof(Index));
            }

            _context.Exercises.Remove(exercise);

            await _context.SaveChangesAsync();
            TempData["Success"] = "Oefening is verwijderd.";
            return RedirectToAction(nameof(Index));
        }
    }
}
