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
    // UI controller voor trainingssessies: lijst met filters + CRUD + sets kopiëren uit workouts.
    [Authorize]
    public class SessionsController : Controller
    {
        private readonly AppDbContext _context; // EF Core databank.
        private readonly ILogger<SessionsController> _logger; // Logging voor fouten en acties.

        public SessionsController(AppDbContext context, ILogger<SessionsController> logger)
        {
            _context = context; // DI DbContext.
            _logger = logger; // DI logger.
        }

        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; // OwnerId uit claims.

        public async Task<IActionResult> Index(string? search, DateTime? from, DateTime? to, string? sort)
        {
            var userId = CurrentUserId; // Alles is per gebruiker.

            var query = _context.Sessions
                .Where(s => s.OwnerId == userId && !s.IsDeleted)
                .Include(s => s.Sets) // Sets mee voor telling/overzicht.
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = $"%{search.Trim()}%";
                query = query.Where(s => EF.Functions.Like(s.Title, pattern)); // Zoeken op titel.
            }

            if (from.HasValue)
            {
                var fromDate = from.Value.Date;
                query = query.Where(s => s.Date >= fromDate); // Vanaf datum (inclusief).
            }

            if (to.HasValue)
            {
                var toExclusive = to.Value.Date.AddDays(1);
                query = query.Where(s => s.Date < toExclusive); // Tot datum (inclusief) via < volgende dag.
            }

            sort = string.IsNullOrWhiteSpace(sort) ? "date_desc" : sort; // Default sort.

            query = sort switch
            {
                "date_asc" => query.OrderBy(s => s.Date).ThenBy(s => s.Title),
                "title_asc" => query.OrderBy(s => s.Title),
                "title_desc" => query.OrderByDescending(s => s.Title),
                _ => query.OrderByDescending(s => s.Date).ThenBy(s => s.Title),
            }; // Sorteert resultaten.

            ViewData["Search"] = search ?? ""; // Bewaart filters in de UI.
            ViewData["From"] = from?.ToString("yyyy-MM-dd") ?? "";
            ViewData["To"] = to?.ToString("yyyy-MM-dd") ?? "";
            ViewData["Sort"] = sort;

            var sessions = await query.ToListAsync(); // Data ophalen.
            return View(sessions); // Overzicht tonen.
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound(); // Geen id => 404.

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .AsNoTracking()
                .Include(s => s.Sets)
                    .ThenInclude(ss => ss.Exercise) // Toon oefeningnamen in details.
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound(); // Niet gevonden/geen rechten.

            if (session.Sets != null)
                session.Sets = session.Sets.OrderBy(x => x.SetNumber).ToList(); // Sets netjes sorteren.

            return View(session); // Detailpagina.
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = CurrentUserId;
            await LoadWorkoutsIntoViewBag(userId); // Dropdown/checkbox lijst van workouts.

            return View(new Session
            {
                Date = DateTime.Today // Default datum = vandaag.
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Date,Description")] Session session, int[] selectedWorkoutIds)
        {
            var userId = CurrentUserId;

            if (!ModelState.IsValid)
            {
                await LoadWorkoutsIntoViewBag(userId); // Workouts opnieuw laden bij fout.
                return View(session);
            }

            session.OwnerId = userId; // Koppelt sessie aan user.
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsDeleted = false; // Soft delete.

            _context.Sessions.Add(session); // Insert sessie.

            try
            {
                await _context.SaveChangesAsync(); // Sessie id wordt nu beschikbaar.

                if (selectedWorkoutIds != null && selectedWorkoutIds.Length > 0)
                {
                    await ReplaceSetsFromWorkoutsAsync(session.Id, userId, selectedWorkoutIds); // Sets vullen vanuit workouts.
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Session aangemaakt + sets uit workouts. SessionId={SessionId} OwnerId={OwnerId} WorkoutsSelected={Count}",
                        session.Id, userId, selectedWorkoutIds.Length);
                }
                else
                {
                    _logger.LogInformation("Session aangemaakt. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                }

                return RedirectToAction(nameof(Index)); // Terug naar overzicht.
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
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted); // Enkel eigen sessie.

            if (session == null) return NotFound();

            await LoadWorkoutsIntoViewBag(userId); // Workouts tonen om sets te kunnen vervangen.
            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Description")] Session formSession, int[] selectedWorkoutIds)
        {
            if (id != formSession.Id) return NotFound(); // Anti-tamper check.

            var userId = CurrentUserId;

            if (!ModelState.IsValid)
            {
                await LoadWorkoutsIntoViewBag(userId);
                return View(formSession);
            }

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            session.Title = formSession.Title; // Velden updaten.
            session.Date = formSession.Date;
            session.Description = formSession.Description;
            session.UpdatedAt = DateTime.UtcNow;

            try
            {
                if (selectedWorkoutIds != null && selectedWorkoutIds.Length > 0)
                {
                    await ReplaceSetsFromWorkoutsAsync(session.Id, userId, selectedWorkoutIds); // Sets volledig vervangen.
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Session aangepast + sets vervangen uit workouts. SessionId={SessionId} OwnerId={OwnerId} WorkoutsSelected={Count}",
                        session.Id, userId, selectedWorkoutIds.Length);
                }
                else
                {
                    await _context.SaveChangesAsync(); // Gewoon sessie opslaan.
                    _logger.LogInformation("Session aangepast. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                }

                return RedirectToAction(nameof(Details), new { id = session.Id }); // Terug naar details.
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
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted); // Enkel eigen sessie.

            if (session == null) return NotFound();

            return View(session); // Delete confirm pagina.
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
                session.IsDeleted = true; // Soft delete.
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
                .ToListAsync(); // Lijst voor selectie op create/edit.

            ViewBag.Workouts = workouts; // Wordt in de view gebruikt.
        }

        private async Task ReplaceSetsFromWorkoutsAsync(int sessionId, string userId, int[] selectedWorkoutIds)
        {
            var allowedWorkoutIds = await _context.Workouts
                .Where(w => w.OwnerId == userId && !w.IsDeleted)
                .Select(w => w.Id)
                .ToListAsync(); // Alleen eigen workouts zijn geldig.

            var validIds = selectedWorkoutIds
                .Where(id => allowedWorkoutIds.Contains(id))
                .Distinct()
                .ToList(); // Filtert ongeldige ids weg.

            if (validIds.Count == 0) return; // Niets om te kopiëren.

            var existingSets = await _context.SessionSets
                .Where(ss => ss.SessionId == sessionId)
                .ToListAsync(); // Oude sets ophalen.

            if (existingSets.Count > 0)
                _context.SessionSets.RemoveRange(existingSets); // Oude sets verwijderen.

            var workoutExercises = await _context.WorkoutExercises
                .Where(we => validIds.Contains(we.WorkoutId))
                .OrderBy(we => we.WorkoutId)
                .ThenBy(we => we.ExerciseId)
                .ToListAsync(); // Haalt alle oefeningen uit geselecteerde workouts.

            var newSets = new List<SessionSet>();
            int setNumber = 1; // Set nummers opnieuw opbouwen.

            foreach (var we in workoutExercises)
            {
                newSets.Add(new SessionSet
                {
                    SessionId = sessionId,
                    SetNumber = setNumber++,
                    ExerciseId = we.ExerciseId,
                    Reps = we.Reps,
                    Weight = we.WeightKg ?? 0.0 // Null weight wordt 0.
                });
            }

            if (newSets.Count > 0)
                _context.SessionSets.AddRange(newSets); // Nieuwe sets toevoegen.
        }
    }
}
