using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;
using WorkoutCoachV2.Web.Models;

namespace WorkoutCoachV2.Web.Controllers
{
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;

        public SessionsController(AppDbContext context)
        {
            _context = context;
        }

        
        public async Task<IActionResult> Index()
        {
            var sessions = await _context.Sessions
                .Include(s => s.Sets)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            return View(sessions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Sets)
                    .ThenInclude(ss => ss.Exercise)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        public IActionResult Create()
        {
            var session = new Session
            {
                Date = DateTime.Today
            };

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Date,Description")] Session session)
        {
            if (!ModelState.IsValid)
            {
                return View(session);
            }

            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsDeleted = false;

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateFromWorkouts()
        {
            var workouts = await _context.Workouts
                .OrderBy(w => w.Title)
                .ToListAsync();

            var vm = new SessionCreateFromWorkoutsViewModel
            {
                Date = DateTime.Today,
                Title = "Session from workouts",
                Workouts = workouts
                    .Select(w => new SessionWorkoutRowViewModel
                    {
                        WorkoutId = w.Id,
                        WorkoutTitle = w.Title,
                        IsSelected = false
                    })
                    .ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFromWorkouts(SessionCreateFromWorkoutsViewModel model)
        {
            if (model.Workouts == null || !model.Workouts.Any(w => w.IsSelected))
            {
                ModelState.AddModelError(string.Empty, "Select at least one workout.");
            }

            if (!ModelState.IsValid)
            {
                var workouts = await _context.Workouts
                    .OrderBy(w => w.Title)
                    .ToListAsync();

                var selected = model.Workouts?.Where(w => w.IsSelected)
                    .ToDictionary(w => w.WorkoutId, w => w.IsSelected)
                    ?? new Dictionary<int, bool>();

                model.Workouts = workouts
                    .Select(w => new SessionWorkoutRowViewModel
                    {
                        WorkoutId = w.Id,
                        WorkoutTitle = w.Title,
                        IsSelected = selected.ContainsKey(w.Id) && selected[w.Id]
                    })
                    .ToList();

                return View(model);
            }

            var session = new Session
            {
                Title = string.IsNullOrWhiteSpace(model.Title)
                    ? "Session from workouts"
                    : model.Title,
                Date = model.Date,
                Description = model.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var selectedIds = model.Workouts
                .Where(w => w.IsSelected)
                .Select(w => w.WorkoutId)
                .ToList();

            var workoutExercises = await _context.WorkoutExercises
                .Include(we => we.Exercise)
                .Where(we => selectedIds.Contains(we.WorkoutId))
                .OrderBy(we => we.WorkoutId)
                .ThenBy(we => we.Exercise.Name)
                .ToListAsync();

            var sessionSets = new List<SessionSet>();
            var setNumber = 1;

            foreach (var we in workoutExercises)
            {
                var set = new SessionSet
                {
                    SessionId = session.Id,
                    ExerciseId = we.ExerciseId,
                    SetNumber = setNumber++,
                    Reps = we.Reps,
                    Weight = we.WeightKg ?? 0,
                    Rpe = null,
                    Note = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                sessionSets.Add(set);
            }

            if (sessionSets.Count > 0)
            {
                _context.SessionSets.AddRange(sessionSets);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = session.Id });
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var session = await _context.Sessions
                .Include(s => s.Sets)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            var workouts = await _context.Workouts
                .OrderBy(w => w.Title)
                .Include(w => w.Exercises)
                .ToListAsync();

            var vm = new SessionCreateFromWorkoutsViewModel
            {
                Title = session.Title,
                Date = session.Date,
                Description = session.Description,
                Workouts = workouts.Select(w => new SessionWorkoutRowViewModel
                {
                    WorkoutId = w.Id,
                    WorkoutTitle = w.Title,
                    IsSelected = w.Exercises.Any(we => session.Sets.Any(ss => ss.ExerciseId == we.ExerciseId))
                }).ToList()
            };

            ViewBag.SessionId = session.Id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SessionCreateFromWorkoutsViewModel model)
        {
            var session = await _context.Sessions
                .Include(s => s.Sets)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null) return NotFound();

            if (model.Workouts == null || !model.Workouts.Any(w => w.IsSelected))
            {
                ModelState.AddModelError(string.Empty, "Select at least one workout.");
            }

            if (!ModelState.IsValid)
            {
                var workouts = await _context.Workouts
                    .OrderBy(w => w.Title)
                    .ToListAsync();

                var selected = model.Workouts?.ToDictionary(x => x.WorkoutId, x => x.IsSelected)
                               ?? new Dictionary<int, bool>();

                model.Workouts = workouts.Select(w => new SessionWorkoutRowViewModel
                {
                    WorkoutId = w.Id,
                    WorkoutTitle = w.Title,
                    IsSelected = selected.ContainsKey(w.Id) && selected[w.Id]
                }).ToList();

                ViewBag.SessionId = id;
                return View(model);
            }

            session.Title = model.Title;
            session.Date = model.Date;
            session.Description = model.Description;
            session.UpdatedAt = DateTime.UtcNow;

            _context.SessionSets.RemoveRange(session.Sets);
            session.Sets.Clear();

            var selectedIds = model.Workouts
                .Where(w => w.IsSelected)
                .Select(w => w.WorkoutId)
                .ToList();

            var workoutExercises = await _context.WorkoutExercises
                .Include(we => we.Exercise)
                .Where(we => selectedIds.Contains(we.WorkoutId))
                .OrderBy(we => we.WorkoutId)
                .ThenBy(we => we.Exercise.Name)
                .ToListAsync();

            int setNumber = 1;
            foreach (var we in workoutExercises)
            {
                session.Sets.Add(new SessionSet
                {
                    SessionId = session.Id,
                    ExerciseId = we.ExerciseId,
                    SetNumber = setNumber++,
                    Reps = we.Reps,
                    Weight = we.WeightKg ?? 0,
                    Rpe = null,
                    Note = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .Include(s => s.Sets)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session != null)
            {
                session.IsDeleted = true;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SessionExists(int id)
        {
            return _context.Sessions.Any(e => e.Id == id);
        }
    }
}
