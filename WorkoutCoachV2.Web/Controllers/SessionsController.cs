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
    [Authorize]
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<SessionsController> _logger;

        public SessionsController(AppDbContext context, ILogger<SessionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        
        public async Task<IActionResult> Index(string? search, DateTime? from, DateTime? to, string? sort)
        {
            var userId = CurrentUserId;

            var query = _context.Sessions
                .Where(s => s.OwnerId == userId && !s.IsDeleted)
                .Include(s => s.Sets)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(s => EF.Functions.Like(s.Title, pattern));
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(s => s.Date >= fromDate);
            }

            if (to.HasValue)
            {
                var toExclusive = to.Value.Date.AddDays(1);
                query = query.Where(s => s.Date < toExclusive);
            }

            sort = string.IsNullOrWhiteSpace(sort) ? "date_desc" : sort;

            query = sort switch
            {
                "date_asc" => query.OrderBy(s => s.Date).ThenBy(s => s.Title),
                "title_asc" => query.OrderBy(s => s.Title),
                "title_desc" => query.OrderByDescending(s => s.Title),
                _ => query.OrderByDescending(s => s.Date).ThenBy(s => s.Title),
            };

            ViewData["Search"] = search ?? "";
            ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? "";
            ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? "";
            ViewData["Sort"] = sort;

            var sessions = await query.ToListAsync();
            return View(sessions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Sets)
                    .ThenInclude(ss => ss.Exercise)
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            if (session.Sets != null)
                session.Sets = session.Sets.OrderBy(x => x.SetNumber).ToList();

            return View(session);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = CurrentUserId;
            await LoadWorkoutsIntoViewBag(userId);

            return View(new Session
            {
                Date = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Date,Description")] Session session, int[] selectedWorkoutIds)
        {
            var userId = CurrentUserId;

            if (!ModelState.IsValid)
            {
                await LoadWorkoutsIntoViewBag(userId);
                return View(session);
            }

            session.OwnerId = userId;
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsDeleted = false;

            _context.Sessions.Add(session);

            try
            {
                await _context.SaveChangesAsync();

                if (selectedWorkoutIds != null && selectedWorkoutIds.Length > 0)
                {
                    await ReplaceSetsFromWorkoutsAsync(session.Id, userId, selectedWorkoutIds);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Session aangemaakt + sets uit workouts. SessionId={SessionId} OwnerId={OwnerId} WorkoutsSelected={Count}",
                        session.Id, userId, selectedWorkoutIds.Length);
                }
                else
                {
                    _logger.LogInformation("Session aangemaakt. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Session Create. OwnerId={OwnerId}", userId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                await LoadWorkoutsIntoViewBag(userId);
                return View(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Session Create. OwnerId={OwnerId}", userId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                await LoadWorkoutsIntoViewBag(userId);
                return View(session);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            await LoadWorkoutsIntoViewBag(userId);
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Description")] Session formSession, int[] selectedWorkoutIds)
        {
            if (id != formSession.Id) return NotFound();

            var userId = CurrentUserId;

            if (!ModelState.IsValid)
            {
                await LoadWorkoutsIntoViewBag(userId);
                return View(formSession);
            }

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            session.Title = formSession.Title;
            session.Date = formSession.Date;
            session.Description = formSession.Description;
            session.UpdatedAt = DateTime.UtcNow;

            try
            {
                if (selectedWorkoutIds != null && selectedWorkoutIds.Length > 0)
                {
                    await ReplaceSetsFromWorkoutsAsync(session.Id, userId, selectedWorkoutIds);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "Session aangepast + sets vervangen uit workouts. SessionId={SessionId} OwnerId={OwnerId} WorkoutsSelected={Count}",
                        session.Id, userId, selectedWorkoutIds.Length);
                }
                else
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Session aangepast. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                }

                return RedirectToAction(nameof(Details), new { id = session.Id });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Session Edit. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                await LoadWorkoutsIntoViewBag(userId);
                return View(formSession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Session Edit. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                await LoadWorkoutsIntoViewBag(userId);
                return View(formSession);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            return View(session);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = CurrentUserId;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            try
            {
                session.IsDeleted = true;
                session.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Session verwijderd (soft). SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Session Delete. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                TempData["Error"] = "Er ging iets mis bij het verwijderen in de databank.";
                return RedirectToAction(nameof(Delete), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Session Delete. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                TempData["Error"] = "Er ging iets mis bij het verwijderen.";
                return RedirectToAction(nameof(Delete), new { id });
            }
        }

        private async Task LoadWorkoutsIntoViewBag(string userId)
        {
            var workouts = await _context.Workouts
                .Where(w => w.OwnerId == userId && !w.IsDeleted)
                .OrderByDescending(w => w.ScheduledOn)
                .ThenBy(w => w.Title)
                .ToListAsync();

            ViewBag.Workouts = workouts;
        }

        private async Task ReplaceSetsFromWorkoutsAsync(int sessionId, string userId, int[] selectedWorkoutIds)
        {
            var allowedWorkoutIds = await _context.Workouts
                .Where(w => w.OwnerId == userId && !w.IsDeleted)
                .Select(w => w.Id)
                .ToListAsync();

            var validIds = selectedWorkoutIds
                .Where(id => allowedWorkoutIds.Contains(id))
                .Distinct()
                .ToList();

            if (validIds.Count == 0) return;

            var existingSets = await _context.SessionSets
                .Where(ss => ss.SessionId == sessionId)
                .ToListAsync();

            if (existingSets.Count > 0)
                _context.SessionSets.RemoveRange(existingSets);

            var workoutExercises = await _context.WorkoutExercises
                .Where(we => validIds.Contains(we.WorkoutId))
                .OrderBy(we => we.WorkoutId)
                .ThenBy(we => we.ExerciseId)
                .ToListAsync();

            var newSets = new List<SessionSet>();
            int setNumber = 1;

            foreach (var we in workoutExercises)
            {
                newSets.Add(new SessionSet
                {
                    SessionId = sessionId,
                    SetNumber = setNumber++,
                    ExerciseId = we.ExerciseId,
                    Reps = we.Reps,
                    Weight = we.WeightKg ?? 0.0
                });
            }

            if (newSets.Count > 0)
                _context.SessionSets.AddRange(newSets);
        }
    }
}
