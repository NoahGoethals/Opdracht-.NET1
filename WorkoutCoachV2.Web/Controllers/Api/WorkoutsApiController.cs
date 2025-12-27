using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // CRUD API voor workouts (zonder sets/exercises details).
[Route("api/workouts")] // api/workouts/...
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // JWT vereist.
public class WorkoutsApiController : ControllerBase
{
    private readonly AppDbContext _db; // EF Core DbContext.

    public WorkoutsApiController(AppDbContext db) => _db = db; // DI.

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; // OwnerId uit JWT.

    [HttpGet] // GET: lijst met search + sort.
    public async Task<ActionResult<List<WorkoutDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sort = "title")
    {
        var q = _db.Workouts // Owner + not deleted.
            .Where(w => w.OwnerId == UserId && !w.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search)) // Titel filter.
            q = q.Where(w => w.Title.Contains(search));

        q = sort switch // Sortering.
        {
            "title_desc" => q.OrderByDescending(w => w.Title),
            "scheduled" => q.OrderBy(w => w.ScheduledOn).ThenBy(w => w.Title),
            _ => q.OrderBy(w => w.Title)
        };

        var items = await q // Projectie naar DTO.
            .Select(w => new WorkoutDto(w.Id, w.Title, w.ScheduledOn))
            .ToListAsync();

        return Ok(items); // 200 + list.
    }

    [HttpGet("{id:int}")] // GET: één workout (owner + not deleted).
    public async Task<ActionResult<WorkoutDto>> GetOne(int id)
    {
        var w = await _db.Workouts
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (w == null) return NotFound(); // 404.

        return Ok(new WorkoutDto(w.Id, w.Title, w.ScheduledOn)); // DTO.
    }

    [HttpPost] // POST: workout aanmaken.
    public async Task<ActionResult<WorkoutDto>> Create([FromBody] CreateWorkoutDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) // Minimale validatie.
            return BadRequest("Title is required.");

        var w = new Workout // Entity opbouwen.
        {
            Title = dto.Title.Trim(),
            ScheduledOn = dto.ScheduledOn,
            OwnerId = UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.Workouts.Add(w); // Insert.
        await _db.SaveChangesAsync(); // Persist.

        return CreatedAtAction(nameof(GetOne), new { id = w.Id }, // 201 + location.
            new WorkoutDto(w.Id, w.Title, w.ScheduledOn));
    }

    [HttpPut("{id:int}")] // PUT: workout updaten.
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkoutDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title)) // Minimale validatie.
            return BadRequest("Title is required.");

        var w = await _db.Workouts // Owner + not deleted.
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (w == null) return NotFound(); // 404.

        w.Title = dto.Title.Trim(); // Update velden.
        w.ScheduledOn = dto.ScheduledOn;
        w.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(); // Persist.
        return NoContent(); // 204.
    }

    [HttpDelete("{id:int}")] // DELETE: soft delete workout.
    public async Task<IActionResult> Delete(int id)
    {
        var w = await _db.Workouts // Owner + not deleted.
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (w == null) return NotFound(); // 404.

        w.IsDeleted = true; // Soft delete flag.
        w.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(); // Persist.
        return NoContent(); // 204.
    }
}
