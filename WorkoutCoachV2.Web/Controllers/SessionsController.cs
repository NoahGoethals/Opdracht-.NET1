using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

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
                .OrderByDescending(s => s.Date)
                .ToListAsync();

            return View(sessions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var session = await _context.Sessions
                .Include(s => s.Sets)
                    .ThenInclude(set => set.Exercise)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

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
                return View(session);

            session.CreatedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;
            session.IsDeleted = false;

            _context.Add(session);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

            return View(session);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Date,Description")] Session formSession)
        {
            if (id != formSession.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(formSession);

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

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

            var session = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == id);

            if (session == null)
                return NotFound();

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
            return _context.Sessions
                .IgnoreQueryFilters()
                .Any(e => e.Id == id);
        }
    }
}
