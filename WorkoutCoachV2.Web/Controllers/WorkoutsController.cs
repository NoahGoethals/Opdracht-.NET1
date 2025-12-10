using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    public class WorkoutsController : Controller
    {
        private readonly AppDbContext _context;

        public WorkoutsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var workouts = await _context.Workouts
                .OrderBy(w => w.ScheduledOn ?? DateTime.MaxValue)
                .ToListAsync();

            return View(workouts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
                return NotFound();

            return View(workout);
        }

        public IActionResult Create()
        {
            var workout = new Workout
            {
                ScheduledOn = DateTime.Today
            };

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,ScheduledOn")] Workout workout)
        {
            if (!ModelState.IsValid)
                return View(workout);

            workout.CreatedAt = DateTime.Now;
            workout.UpdatedAt = DateTime.Now;
            workout.IsDeleted = false;

            _context.Add(workout);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
                return NotFound();

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ScheduledOn")] Workout formWorkout)
        {
            if (id != formWorkout.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(formWorkout);

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
                return NotFound();

            workout.Title = formWorkout.Title;
            workout.ScheduledOn = formWorkout.ScheduledOn;
            workout.UpdatedAt = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkoutExists(workout.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var workout = await _context.Workouts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout == null)
                return NotFound();

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workout = await _context.Workouts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workout != null && !workout.IsDeleted)
            {
                workout.IsDeleted = true;
                workout.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool WorkoutExists(int id)
        {
            return _context.Workouts
                .IgnoreQueryFilters()
                .Any(e => e.Id == id);
        }
    }
}
