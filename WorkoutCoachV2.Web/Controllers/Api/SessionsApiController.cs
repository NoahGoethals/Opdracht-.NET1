using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // CRUD API voor sessies + optioneel sets ophalen.
[Route("api/sessions")] // api/sessions/...
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // JWT vereist.
public class SessionsApiController : ControllerBase
{
    private readonly AppDbContext _db; // EF Core DbContext.

    public SessionsApiController(AppDbContext db) => _db = db; // DI.

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier); // OwnerId uit JWT.

    [HttpGet] // GET: lijst met filters (search/date range/sort) + optioneel includeSets.
    public async Task<ActionResult<List<SessionDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sort = "date_desc",
        [FromQuery] bool includeSets = false)
    {
        if (UserId is null) return Unauthorized(); // Geen user claim => 401.

        var q = _db.Sessions // Basis query: owner + niet soft-deleted.
            .AsNoTracking()
            .Where(s => s.OwnerId == UserId && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search)) // Titel filter.
            q = q.Where(s => s.Title.Contains(search));

        if (from.HasValue) // Vanaf datum filter.
            q = q.Where(s => s.Date >= from.Value);

        if (to.HasValue) // Tot datum filter.
            q = q.Where(s => s.Date <= to.Value);

        q = sort switch // Sortering.
        {
            "date_asc" => q.OrderBy(s => s.Date),
            "title" => q.OrderBy(s => s.Title),
            _ => q.OrderByDescending(s => s.Date),
        };

        if (includeSets) // Alleen includen als client het vraagt.
            q = q.Include(s => s.Sets);

        var list = await q.ToListAsync(); // Async ophalen.

        var result = list.Select(s => // Mapping naar SessionDto.
        {
            var sets = includeSets
                ? (s.Sets ?? new List<SessionSet>())
                    .OrderBy(x => x.ExerciseId)
                    .ThenBy(x => x.SetNumber)
                    .Select(x => new SessionSetDto(
                        ExerciseId: x.ExerciseId,
                        SetNumber: x.SetNumber,
                        Reps: x.Reps,
                        Weight: x.Weight
                    ))
                    .ToList()
                : new List<SessionSetDto>();

            return new SessionDto(
                Id: s.Id,
                Title: s.Title,
                Date: s.Date,
                Description: s.Description,
                SetsCount: (s.Sets?.Count ?? 0),
                Sets: sets
            );
        }).ToList();

        return Ok(result); // 200 + lijst.
    }

    [HttpGet("{id:int}")] // GET: één sessie + sets.
    public async Task<ActionResult<SessionDto>> GetOne(int id)
    {
        if (UserId is null) return Unauthorized(); // 401.

        var session = await _db.Sessions // Owner + not deleted, met sets.
            .AsNoTracking()
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == UserId && !s.IsDeleted);

        if (session is null) return NotFound(); // 404.

        var dto = new SessionDto( // Mapping naar DTO.
            Id: session.Id,
            Title: session.Title,
            Date: session.Date,
            Description: session.Description,
            SetsCount: session.Sets.Count,
            Sets: session.Sets
                .OrderBy(x => x.ExerciseId)
                .ThenBy(x => x.SetNumber)
                .Select(x => new SessionSetDto(
                    ExerciseId: x.ExerciseId,
                    SetNumber: x.SetNumber,
                    Reps: x.Reps,
                    Weight: x.Weight
                ))
                .ToList()
        );

        return Ok(dto); // 200 + object.
    }

    [HttpPost] // POST: sessie aanmaken + sets (met ExerciseId validatie op owner).
    public async Task<ActionResult<SessionDto>> Create([FromBody] UpsertSessionDto dto)
    {
        if (UserId is null) return Unauthorized(); // 401.
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required."); // Input validatie.

        var sets = dto.Sets ?? new List<SessionSetDto>(); // Null-safe.

        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList(); // Unieke oefeningen.
        if (exerciseIds.Count > 0)
        {
            var validCount = await _db.Exercises // Alleen oefeningen van owner tellen.
                .Where(e => e.OwnerId == UserId && exerciseIds.Contains(e.Id))
                .CountAsync();

            if (validCount != exerciseIds.Count)
                return BadRequest("One or more ExerciseId values are invalid for this user.");
        }

        var session = new Session // Basis sessie entity.
        {
            OwnerId = UserId,
            Title = dto.Title.Trim(),
            Date = dto.Date,
            Description = dto.Description
        };

        _db.Sessions.Add(session); // Insert sessie.
        await _db.SaveChangesAsync(); // Eerst saven om SessionId te krijgen.

        foreach (var s in sets) // Sets toevoegen met defensieve minwaarden.
        {
            session.Sets.Add(new SessionSet
            {
                SessionId = session.Id,
                ExerciseId = s.ExerciseId,
                SetNumber = Math.Max(1, s.SetNumber),
                Reps = Math.Max(0, s.Reps),
                Weight = Math.Max(0.0, s.Weight)
            });
        }

        await _db.SaveChangesAsync(); // Persist sets.

        var created = new SessionDto( // DTO response.
            Id: session.Id,
            Title: session.Title,
            Date: session.Date,
            Description: session.Description,
            SetsCount: session.Sets.Count,
            Sets: session.Sets
                .OrderBy(x => x.ExerciseId)
                .ThenBy(x => x.SetNumber)
                .Select(x => new SessionSetDto(x.ExerciseId, x.SetNumber, x.Reps, x.Weight))
                .ToList()
        );

        return CreatedAtAction(nameof(GetOne), new { id = session.Id }, created); // 201 + location.
    }

    [HttpPut("{id:int}")] // PUT: sessie updaten + sets vervangen (remove + add).
    public async Task<IActionResult> Update(int id, [FromBody] UpsertSessionDto dto)
    {
        if (UserId is null) return Unauthorized(); // 401.

        var session = await _db.Sessions // Session laden + sets voor replace.
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == UserId && !s.IsDeleted);

        if (session is null) return NotFound(); // 404.
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required."); // Validatie.

        var sets = dto.Sets ?? new List<SessionSetDto>(); // Null-safe.

        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList(); // Unieke oefeningen.
        if (exerciseIds.Count > 0)
        {
            var validCount = await _db.Exercises // ExerciseIds moeten van owner zijn.
                .Where(e => e.OwnerId == UserId && exerciseIds.Contains(e.Id))
                .CountAsync();

            if (validCount != exerciseIds.Count)
                return BadRequest("One or more ExerciseId values are invalid for this user.");
        }

        session.Title = dto.Title.Trim(); // Basisvelden updaten.
        session.Date = dto.Date;
        session.Description = dto.Description;

        if (session.Sets.Count > 0) // Oude sets verwijderen voor volledige replace.
            _db.SessionSets.RemoveRange(session.Sets);

        foreach (var s in sets) // Nieuwe sets toevoegen.
        {
            _db.SessionSets.Add(new SessionSet
            {
                SessionId = session.Id,
                ExerciseId = s.ExerciseId,
                SetNumber = Math.Max(1, s.SetNumber),
                Reps = Math.Max(0, s.Reps),
                Weight = Math.Max(0.0, s.Weight)
            });
        }

        await _db.SaveChangesAsync(); // Persist.
        return NoContent(); // 204.
    }

    [HttpDelete("{id:int}")] // DELETE: soft delete sessie.
    public async Task<IActionResult> Delete(int id)
    {
        if (UserId is null) return Unauthorized(); // 401.

        var session = await _db.Sessions // Session laden (owner).
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == UserId);

        if (session is null) return NotFound(); // 404.
        if (session.IsDeleted) return NotFound(); // Reeds verwijderd => 404.

        session.IsDeleted = true; // Soft delete flag.
        await _db.SaveChangesAsync(); // Persist.

        return NoContent(); // 204.
    }
}
