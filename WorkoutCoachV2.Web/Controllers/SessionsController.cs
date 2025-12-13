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
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;

        public SessionsController(AppDbContext context)
        {
            _context = context;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;

            var sessions = await _context.Sessions
                .Include(s => s.Sets)
                .Where(s => s.OwnerId == userId)
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            return View(sessions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .Include(s => s.Sets)
                    .ThenInclude(ss => ss.Exercise)
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId);

            if (session == null) return NotFound();

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

            var userId = CurrentUserId;

            session.OwnerId = userId;
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
            var userId = CurrentUserId;

            var workouts = await _context.Workouts
                .Where(w => w.OwnerId == userId)
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
            var userId = CurrentUserId;

            if (model.Workouts == null || !model.Workouts.Any(w => w.IsSelected))
            {
                ModelState.AddModelError(string.Empty, "Selecteer minstens één workout.");
            }

            if (!ModelState.IsValid)
            {
                var workouts = await _context.Workouts
                    .Where(w => w.OwnerId == userId)
                    .OrderBy(w => w.Title)
                    .ToListAsync();

                var selected = model.Workouts?
                    .ToDictionary(w => w.WorkoutId, w => w.IsSelected)
                    ?? new Dictionary<int, bool>();

                model.Workouts = workouts
                    .Select(w => new SessionWorkoutRowViewModel
                    {
                        WorkoutId = w.Id,
                        WorkoutTitle = w.Title,
                        IsSelected = selected.TryGetValue(w.Id, out var isSel) && isSel
                    })
                    .ToList();

                return View(model);
            }

            var session = new Session
            {
                Title = string.IsNullOrWhiteSpace(model.Title) ? "Session from workouts" : model.Title,
                Date = model.Date,
                Description = model.Description,
                OwnerId = userId,
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

            selectedIds = await _context.Workouts
                .Where(w => w.OwnerId == userId && selectedIds.Contains(w.Id))
                .Select(w => w.Id)
                .ToListAsync();

            var workoutExercises = await _context.WorkoutExercises
                .Where(we => selectedIds.Contains(we.WorkoutId))
                .OrderBy(we => we.WorkoutId)
                .ThenBy(we => we.ExerciseId)
                .ToListAsync();

            var setNumber = 1;
            var sessionSets = workoutExercises.Select(we => new SessionSet
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
            }).ToList();

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

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId);

            if (session == null) return NotFound();

            var workouts = await _context.Workouts
                .Where(w => w.OwnerId == userId)
                .OrderBy(w => w.Title)
                .ToListAsync();

            var vm = new SessionEditViewModel
            {
                Id = session.Id,
                Title = session.Title,
                Date = session.Date,
                Description = session.Description,

                Workouts = workouts.Select(w => new SessionWorkoutRowViewModel
                {
                    WorkoutId = w.Id,
                    WorkoutTitle = w.Title,
                    IsSelected = false
                }).ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SessionEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            var userId = CurrentUserId;

            if (!ModelState.IsValid)
            {
                var workouts = await _context.Workouts
                    .Where(w => w.OwnerId == userId)
                    .OrderBy(w => w.Title)
                    .ToListAsync();

                var selected = model.Workouts?
                    .ToDictionary(x => x.WorkoutId, x => x.IsSelected)
                    ?? new Dictionary<int, bool>();

                model.Workouts = workouts.Select(w => new SessionWorkoutRowViewModel
                {
                    WorkoutId = w.Id,
                    WorkoutTitle = w.Title,
                    IsSelected = selected.TryGetValue(w.Id, out var isSel) && isSel
                }).ToList();

                return View(model);
            }

            var session = await _context.Sessions
                .Include(s => s.Sets)
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId);

            if (session == null) return NotFound();

            session.Title = model.Title;
            session.Date = model.Date;
            session.Description = model.Description;
            session.UpdatedAt = DateTime.UtcNow;

            var selectedWorkoutIds = model.Workouts
                .Where(w => w.IsSelected)
                .Select(w => w.WorkoutId)
                .ToList();

            if (selectedWorkoutIds.Any())
            {
                selectedWorkoutIds = await _context.Workouts
                    .Where(w => w.OwnerId == userId && selectedWorkoutIds.Contains(w.Id))
                    .Select(w => w.Id)
                    .ToListAsync();
            }

            if (selectedWorkoutIds.Any())
            {
                if (session.Sets != null && session.Sets.Any())
                {
                    _context.SessionSets.RemoveRange(session.Sets);
                }

                var workoutExercises = await _context.WorkoutExercises
                    .Where(we => selectedWorkoutIds.Contains(we.WorkoutId))
                    .OrderBy(we => we.WorkoutId)
                    .ThenBy(we => we.ExerciseId)
                    .ToListAsync();

                var setNr = 1;
                var newSets = workoutExercises.Select(we => new SessionSet
                {
                    SessionId = session.Id,
                    ExerciseId = we.ExerciseId,
                    SetNumber = setNr++,
                    Reps = we.Reps,
                    Weight = we.WeightKg ?? 0,
                    Rpe = null,
                    Note = null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsDeleted = false
                }).ToList();

                _context.SessionSets.AddRange(newSets);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .Include(s => s.Sets)
                .FirstOrDefaultAsync(m => m.Id == id && m.OwnerId == userId);

            if (session == null) return NotFound();

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId);

            if (session != null)
            {
                session.IsDeleted = true;
                session.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
