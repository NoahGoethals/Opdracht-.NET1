using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ExercisesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ExercisesController(AppDbContext db) => _db = db;

        private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        public record ExerciseDto(int Id, string Name, string? Category, string? Notes);
        public record CreateExerciseDto(string Name, string? Category, string? Notes);
        public record UpdateExerciseDto(string Name, string? Category, string? Notes);

        [HttpGet]
        public async Task<ActionResult<List<ExerciseDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] string? sort = "name")
        {
            var q = _db.Exercises.Where(e => e.OwnerId == UserId);

            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(e => e.Name.Contains(search));

            if (!string.IsNullOrWhiteSpace(category))
                q = q.Where(e => e.Category == category);

            q = sort switch
            {
                "name_desc" => q.OrderByDescending(e => e.Name),
                "category" => q.OrderBy(e => e.Category).ThenBy(e => e.Name),
                _ => q.OrderBy(e => e.Name)
            };

            var items = await q
                .Select(e => new ExerciseDto(e.Id, e.Name, e.Category, e.Notes))
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ExerciseDto>> GetOne(int id)
        {
            var e = await _db.Exercises.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
            if (e == null) return NotFound();

            return Ok(new ExerciseDto(e.Id, e.Name, e.Category, e.Notes));
        }

        [HttpPost]
        public async Task<ActionResult<ExerciseDto>> Create([FromBody] CreateExerciseDto dto)
        {
            var e = new Exercise
            {
                Name = dto.Name,
                Category = dto.Category ?? "",
                Notes = dto.Notes,
                OwnerId = UserId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Exercises.Add(e);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOne), new { id = e.Id },
                new ExerciseDto(e.Id, e.Name, e.Category, e.Notes));
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExerciseDto dto)
        {
            var e = await _db.Exercises.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
            if (e == null) return NotFound();

            e.Name = dto.Name;
            e.Category = dto.Category ?? "";
            e.Notes = dto.Notes;
            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var e = await _db.Exercises.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == UserId);
            if (e == null) return NotFound();

            e.IsDeleted = true;
            e.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
