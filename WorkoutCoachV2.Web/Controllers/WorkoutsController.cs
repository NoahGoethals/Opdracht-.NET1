using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    [Authorize]
    public class WorkoutsController : Controller
    {
        private readonly AppDbContext _context;

        public WorkoutsController(AppDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;

            // ✅ Include Exercises zodat item.Exercises?.Count klopt in de view
            var workouts = await _context.Workouts
                .Where(w => w.OwnerId == userId)
                .Include(w => w.Exercises)
                .OrderByDescending(w => w.ScheduledOn)
                .ThenBy(w => w.Title)
                .ToListAsync();

            return View(workouts);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId);

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
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId);

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
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId);

            if (workout == null) return NotFound();

            workout.Title = formWorkout.Title;
            workout.ScheduledOn = formWorkout.ScheduledOn;
            workout.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId);

            if (workout == null) return NotFound();

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId);

            if (workout != null)
            {
                workout.IsDeleted = true;
                workout.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Exercises(int id)
        {
            var userId = CurrentUserId;

            var workout = await _context.Workouts
                .Include(w => w.Exercises)
                .ThenInclude(we => we.Exercise)
                .FirstOrDefaultAsync(w => w.Id == id && w.OwnerId == userId);

            if (workout == null) return NotFound();

            var allExercises = await _context.Exercises
                .Where(e => e.OwnerId == userId)
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
                .FirstOrDefaultAsync(w => w.Id == model.WorkoutId && w.OwnerId == userId);

            if (workout == null) return NotFound();

            var allowedExerciseIds = await _context.Exercises
                .Where(e => e.OwnerId == userId)
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
            {
                _context.WorkoutExercises.AddRange(selected);
            }

            workout.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = workout.Id });
        }
    }
}
