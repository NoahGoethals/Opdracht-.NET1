using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/workouts")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkoutsApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkoutsApiController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public record WorkoutDto(int Id, string Title, DateTime? ScheduledOn);
    public record CreateWorkoutDto(string Title, DateTime? ScheduledOn);
    public record UpdateWorkoutDto(string Title, DateTime? ScheduledOn);

    [HttpGet]
    public async Task<ActionResult<List<WorkoutDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? sort = "title")
    {
        var q = _db.Workouts
            .Where(w => w.OwnerId == UserId && !w.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(w => w.Title.Contains(search));

        q = sort switch
        {
            "title_desc" => q.OrderByDescending(w => w.Title),
            "scheduled" => q.OrderBy(w => w.ScheduledOn).ThenBy(w => w.Title),
            _ => q.OrderBy(w => w.Title)
        };

        var items = await q
            .Select(w => new WorkoutDto(w.Id, w.Title, w.ScheduledOn))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkoutDto>> GetOne(int id)
    {
        var w = await _db.Workouts
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (w == null) return NotFound();

        return Ok(new WorkoutDto(w.Id, w.Title, w.ScheduledOn));
    }

    [HttpPost]
    public async Task<ActionResult<WorkoutDto>> Create([FromBody] CreateWorkoutDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var w = new Workout
        {
            Title = dto.Title.Trim(),
            ScheduledOn = dto.ScheduledOn,
            OwnerId = UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        _db.Workouts.Add(w);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetOne), new { id = w.Id },
            new WorkoutDto(w.Id, w.Title, w.ScheduledOn));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkoutDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest("Title is required.");

        var w = await _db.Workouts
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (w == null) return NotFound();

        w.Title = dto.Title.Trim();
        w.ScheduledOn = dto.ScheduledOn;
        w.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var w = await _db.Workouts
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (w == null) return NotFound();

        w.IsDeleted = true;
        w.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
