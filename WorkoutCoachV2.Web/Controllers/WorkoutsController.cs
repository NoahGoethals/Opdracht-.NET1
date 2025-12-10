using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

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
                .Where(w => !w.IsDeleted)
                .OrderBy(w => w.ScheduledOn)
                .ToListAsync();

            return View(workouts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workout == null)
            {
                return NotFound();
            }

            var workoutExercises = await _context.WorkoutExercises
                .Where(we => we.WorkoutId == workout.Id)
                .Include(we => we.Exercise)
                .OrderBy(we => we.Exercise!.Name)
                .ToListAsync();

            ViewBag.WorkoutExercises = workoutExercises;

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
            {
                return View(workout);
            }

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
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,ScheduledOn")] Workout formWorkout)
        {
            if (id != formWorkout.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(formWorkout);
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workout == null)
            {
                return NotFound();
            }

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
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workout != null)
            {
                workout.IsDeleted = true;
                workout.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Exercises(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

            if (workout == null)
            {
                return NotFound();
            }

            var exercises = await _context.Exercises
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.Name)
                .ToListAsync();

            var existing = await _context.WorkoutExercises
                .Where(we => we.WorkoutId == workout.Id)
                .ToListAsync();

            var vm = new WorkoutExercisesEditViewModel
            {
                WorkoutId = workout.Id,
                WorkoutTitle = workout.Title,
                Items = exercises.Select(e =>
                {
                    var we = existing.FirstOrDefault(x => x.ExerciseId == e.Id);
                    return new WorkoutExerciseRowViewModel
                    {
                        ExerciseId = e.Id,
                        ExerciseName = e.Name,
                        IsSelected = we != null,
                        Reps = we?.Reps ?? 0,
                        WeightKg = we?.WeightKg ?? 0
                    };
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Exercises(WorkoutExercisesEditViewModel model)
        {
            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == model.WorkoutId && !w.IsDeleted);

            if (workout == null)
            {
                return NotFound();
            }

            var existing = await _context.WorkoutExercises
                .Where(we => we.WorkoutId == model.WorkoutId)
                .ToListAsync();

            _context.WorkoutExercises.RemoveRange(existing);

            foreach (var item in model.Items.Where(i => i.IsSelected))
            {
                var entity = new WorkoutExercise
                {
                    WorkoutId = model.WorkoutId,
                    ExerciseId = item.ExerciseId,
                    Reps = item.Reps,
                    WeightKg = item.WeightKg
                };

                _context.WorkoutExercises.Add(entity);
            }

            workout.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = model.WorkoutId });
        }

        private bool WorkoutExists(int id)
        {
            return _context.Workouts
                .IgnoreQueryFilters()
                .Any(e => e.Id == id);
        }
    }
}
