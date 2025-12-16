using System;
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

        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;

            var sessions = await _context.Sessions
                .Where(s => s.OwnerId == userId && !s.IsDeleted)
                .Include(s => s.Sets)
                .OrderByDescending(s => s.Date)
                .ThenBy(s => s.Title)
                .ToListAsync();

            return View(sessions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            var sets = await _context.SessionSets
                .AsNoTracking()
                .Include(ss => ss.Exercise)
                .Where(ss => ss.SessionId == session.Id)
                .OrderBy(ss => ss.SetNumber)
                .ToListAsync();

            ViewBag.Sets = sets;

            return View(session);
        }

        public IActionResult Create()
        {
            return View(new Session
            {
                Date = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Date,Description")] Session session)
        {
            if (!ModelState.IsValid) return View(session);

            session.OwnerId = CurrentUserId;
            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsDeleted = false;

            _context.Sessions.Add(session);

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Session aangemaakt. SessionId={SessionId} OwnerId={OwnerId}", session.Id, session.OwnerId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Session Create. OwnerId={OwnerId}", session.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Session Create. OwnerId={OwnerId}", session.OwnerId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(session);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Description")] Session formSession)
        {
            if (id != formSession.Id) return NotFound();
            if (!ModelState.IsValid) return View(formSession);

            var userId = CurrentUserId;

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == userId && !s.IsDeleted);

            if (session == null) return NotFound();

            session.Title = formSession.Title;
            session.Date = formSession.Date;
            session.Description = formSession.Description;
            session.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Session aangepast. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "DbUpdateException bij Session Edit. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan in de databank.");
                return View(formSession);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Onverwachte fout bij Session Edit. SessionId={SessionId} OwnerId={OwnerId}", session.Id, userId);
                ModelState.AddModelError("", "Er ging iets mis bij het opslaan.");
                return View(formSession);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var userId = CurrentUserId;

            var session = await _context.Sessions
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
    }
}
