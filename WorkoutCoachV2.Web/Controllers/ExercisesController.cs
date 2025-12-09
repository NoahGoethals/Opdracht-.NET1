using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    public class ExercisesController : Controller
    {
        private readonly AppDbContext _context;

        public ExercisesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Exercises
        public async Task<IActionResult> Index()
        {
            // Door de global query filter in AppDbContext zien we enkel IsDeleted == false
            var exercises = await _context.Exercises
                .OrderBy(e => e.Name)
                .ToListAsync();

            return View(exercises);
        }

        // GET: Exercises/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exercise == null)
                return NotFound();

            return View(exercise);
        }

        // GET: Exercises/Create
        public IActionResult Create()
        {
            var exercise = new Exercise
            {
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsDeleted = false
            };

            return View(exercise);
        }

        // POST: Exercises/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Category,Notes")] Exercise exercise)
        {
            if (!ModelState.IsValid)
                return View(exercise);

            exercise.CreatedAt = DateTime.Now;
            exercise.UpdatedAt = DateTime.Now;
            exercise.IsDeleted = false;

            _context.Add(exercise);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Exercises/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var exercise = await _context.Exercises.FindAsync(id);
            if (exercise == null)
                return NotFound();

            return View(exercise);
        }

        // POST: Exercises/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,Notes")] Exercise formExercise)
        {
            if (id != formExercise.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(formExercise);

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise == null)
                return NotFound();

            // Velden die de gebruiker mag wijzigen
            exercise.Name = formExercise.Name;
            exercise.Category = formExercise.Category;
            exercise.Notes = formExercise.Notes;

            // System-veld
            exercise.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ExerciseExists(exercise.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Exercises/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var exercise = await _context.Exercises
                .FirstOrDefaultAsync(m => m.Id == id);

            if (exercise == null)
                return NotFound();

            return View(exercise);
        }

        // POST: Exercises/Delete/5  (soft delete)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // IgnoreQueryFilters zodat we hem zeker vinden, ook als hij al IsDeleted == true is
            var exercise = await _context.Exercises
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exercise != null)
            {
                exercise.IsDeleted = true;
                exercise.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ExerciseExists(int id)
        {
            // Hier ook IgnoreQueryFilters, zodat we ook soft-deleted items tellen
            return _context.Exercises
                .IgnoreQueryFilters()
                .Any(e => e.Id == id);
        }
    }
}
