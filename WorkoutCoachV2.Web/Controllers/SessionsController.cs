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

            var exerciseIds = session.Sets
                .Select(ss => ss.ExerciseId)
                .Distinct()
                .ToList();

            var workouts = await _context.Workouts
                .Include(w => w.Exercises)
                    .ThenInclude(we => we.Exercise)
                .Where(w => w.Exercises.Any(we => exerciseIds.Contains(we.ExerciseId)))
                .OrderBy(w => w.Title)
                .ToListAsync();

            var remainingSets = session.Sets
                .OrderBy(ss => ss.SetNumber)
                .ToList();

            var workoutGroups = new List<SessionDetailsWorkoutGroupViewModel>();

            foreach (var workout in workouts)
            {
                var group = new SessionDetailsWorkoutGroupViewModel
                {
                    WorkoutTitle = workout.Title
                };

                foreach (var we in workout.Exercises
                                          .OrderBy(we => we.Exercise.Name))
                {
                    var matchingSet = remainingSets
                        .FirstOrDefault(ss => ss.ExerciseId == we.ExerciseId);

                    if (matchingSet != null)
                    {
                        group.Sets.Add(new SessionDetailsSetRowViewModel
                        {
                            SetNumber = matchingSet.SetNumber,
                            ExerciseName = matchingSet.Exercise?.Name ?? string.Empty,
                            Reps = matchingSet.Reps,
                            Weight = matchingSet.Weight
                        });

                        remainingSets.Remove(matchingSet);
                    }
                }

                if (group.Sets.Any())
                {
                    workoutGroups.Add(group);
                }
            }

            var vm = new SessionDetailsViewModel
            {
                SessionId = session.Id,
                Title = session.Title,
                Date = session.Date,
                Description = session.Description,
                Workouts = workoutGroups,
                ExtraSets = remainingSets
                    .Select(ss => new SessionDetailsSetRowViewModel
                    {
                        SetNumber = ss.SetNumber,
                        ExerciseName = ss.Exercise?.Name ?? string.Empty,
                        Reps = ss.Reps,
                        Weight = ss.Weight
                    })
                    .ToList()
            };

            return View(vm);
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

            var workoutsWithExercises = await _context.Workouts
                .Include(w => w.Exercises)
                    .ThenInclude(we => we.Exercise)
                .Where(w => selectedIds.Contains(w.Id))
                .ToListAsync();

            var sessionSets = new List<SessionSet>();
            var setNumber = 1;

            foreach (var workout in workoutsWithExercises)
            {
                foreach (var we in workout.Exercises
                                          .OrderBy(we => we.Exercise.Name))
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
            if (id == null)
            {
                return NotFound();
            }

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Description")] Session formSession)
        {
            if (id != formSession.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(formSession);
            }

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
            {
                return NotFound();
            }

            session.Title = formSession.Title;
            session.Date = formSession.Date;
            session.Description = formSession.Description;
            session.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SessionExists(session.Id))
                {
                    return NotFound();
                }

                throw;
            }

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
