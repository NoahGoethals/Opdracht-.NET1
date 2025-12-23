using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/sessions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SessionsApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public SessionsApiController(AppDbContext db) => _db = db;

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet]
    public async Task<ActionResult<List<SessionDto>>> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] string? sort = "date_desc",
        [FromQuery] bool includeSets = false)
    {
        if (UserId is null) return Unauthorized();

        var q = _db.Sessions
            .AsNoTracking()
            .Where(s => s.OwnerId == UserId && !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.Title.Contains(search));

        if (from.HasValue)
            q = q.Where(s => s.Date >= from.Value);

        if (to.HasValue)
            q = q.Where(s => s.Date <= to.Value);

        q = sort switch
        {
            "date_asc" => q.OrderBy(s => s.Date),
            "title" => q.OrderBy(s => s.Title),
            _ => q.OrderByDescending(s => s.Date),
        };

        if (includeSets)
            q = q.Include(s => s.Sets);

        var list = await q.ToListAsync();

        var result = list.Select(s =>
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

        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SessionDto>> GetOne(int id)
    {
        if (UserId is null) return Unauthorized();

        var session = await _db.Sessions
            .AsNoTracking()
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == UserId && !s.IsDeleted);

        if (session is null) return NotFound();

        var dto = new SessionDto(
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

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<SessionDto>> Create([FromBody] UpsertSessionDto dto)
    {
        if (UserId is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");

        var sets = dto.Sets ?? new List<SessionSetDto>();

        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList();
        if (exerciseIds.Count > 0)
        {
            var validCount = await _db.Exercises
                .Where(e => e.OwnerId == UserId && exerciseIds.Contains(e.Id))
                .CountAsync();

            if (validCount != exerciseIds.Count)
                return BadRequest("One or more ExerciseId values are invalid for this user.");
        }

        var session = new Session
        {
            OwnerId = UserId,
            Title = dto.Title.Trim(),
            Date = dto.Date,
            Description = dto.Description
        };

        _db.Sessions.Add(session);
        await _db.SaveChangesAsync();

        foreach (var s in sets)
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

        await _db.SaveChangesAsync();

        var created = new SessionDto(
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

        return CreatedAtAction(nameof(GetOne), new { id = session.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpsertSessionDto dto)
    {
        if (UserId is null) return Unauthorized();

        var session = await _db.Sessions
            .Include(s => s.Sets)
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == UserId && !s.IsDeleted);

        if (session is null) return NotFound();
        if (string.IsNullOrWhiteSpace(dto.Title)) return BadRequest("Title is required.");

        var sets = dto.Sets ?? new List<SessionSetDto>();

        var exerciseIds = sets.Select(s => s.ExerciseId).Distinct().ToList();
        if (exerciseIds.Count > 0)
        {
            var validCount = await _db.Exercises
                .Where(e => e.OwnerId == UserId && exerciseIds.Contains(e.Id))
                .CountAsync();

            if (validCount != exerciseIds.Count)
                return BadRequest("One or more ExerciseId values are invalid for this user.");
        }

        session.Title = dto.Title.Trim();
        session.Date = dto.Date;
        session.Description = dto.Description;

        if (session.Sets.Count > 0)
            _db.SessionSets.RemoveRange(session.Sets);

        foreach (var s in sets)
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

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (UserId is null) return Unauthorized();

        var session = await _db.Sessions
            .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == UserId);

        if (session is null) return NotFound();
        if (session.IsDeleted) return NotFound();

        session.IsDeleted = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
