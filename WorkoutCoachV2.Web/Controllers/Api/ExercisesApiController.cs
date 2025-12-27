using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // CRUD API voor oefeningen.
[Route("api/exercises")] // api/exercises/...
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // JWT vereist.
public class ExercisesApiController : ControllerBase
{
    private readonly AppDbContext _db; // EF Core DbContext voor data access.

    public ExercisesApiController(AppDbContext db) => _db = db; // DI.

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; // OwnerId uit JWT.

    [HttpGet] // GET: lijst met filters/sort voor owner.
    public async Task<ActionResult<List<ExerciseDto>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? sort = "name")
    {
        var q = _db.Exercises // Basis query: enkel owner + niet soft-deleted.
            .Where(e => e.OwnerId == UserId && !e.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search)) // Tekstfilter op Name.
            q = q.Where(e => e.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(category)) // Exacte categorie-filter.
            q = q.Where(e => e.Category == category);

        q = sort switch // Sortering: name, name_desc, category.
        {
            "name_desc" => q.OrderByDescending(e => e.Name),
            "category" => q.OrderBy(e => e.Category).ThenBy(e => e.Name),
            _ => q.OrderBy(e => e.Name)
        };

        var items = await q // Projectie naar DTO.
            .Select(e => new ExerciseDto(e.Id, e.Name, e.Category, e.Notes))
            .ToListAsync();

        return Ok(items); // 200 + list.
    }

    [HttpGet("{id:int}")] // GET: één item (owner + not deleted).
    public async Task<ActionResult<ExerciseDto>> GetOne(int id)
    {
        var e = await _db.Exercises // Owner check + soft delete check.
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (e == null) return NotFound(); // 404.

        return Ok(new ExerciseDto(e.Id, e.Name, e.Category, e.Notes)); // DTO.
    }

    [HttpPost] // POST: oefening aanmaken voor owner.
    public async Task<ActionResult<ExerciseDto>> Create([FromBody] CreateExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) // Minimale validatie.
            return BadRequest("Name is required.");

        var e = new Exercise // Entity opbouwen.
        {
            Name = dto.Name.Trim(),
            Category = dto.Category ?? "",
            Notes = dto.Notes,
            OwnerId = UserId, // User-scoping.
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false // Soft delete standaard false.
        };

        _db.Exercises.Add(e); // Insert.
        await _db.SaveChangesAsync(); // Persist.

        return CreatedAtAction(nameof(GetOne), new { id = e.Id }, // 201 + location.
            new ExerciseDto(e.Id, e.Name, e.Category, e.Notes));
    }

    [HttpPut("{id:int}")] // PUT: update (owner + not deleted).
    public async Task<IActionResult> Update(int id, [FromBody] UpdateExerciseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name)) // Minimale validatie.
            return BadRequest("Name is required.");

        var e = await _db.Exercises // Laden met owner filter.
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (e == null) return NotFound();

        e.Name = dto.Name.Trim(); // Updaten.
        e.Category = dto.Category ?? "";
        e.Notes = dto.Notes;
        e.UpdatedAt = DateTime.UtcNow; // Timestamp.

        await _db.SaveChangesAsync(); // Persist.
        return NoContent(); // 204.
    }

    [HttpDelete("{id:int}")] // DELETE: soft delete (owner + not deleted).
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Exercises // Laden met owner filter.
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId && !x.IsDeleted);

        if (e == null) return NotFound();

        e.IsDeleted = true; // Soft delete flag.
        e.UpdatedAt = DateTime.UtcNow; // Timestamp.

        await _db.SaveChangesAsync(); // Persist.
        return NoContent(); // 204.
    }
}
