using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // Legacy endpoint om ook deleted items te zien.
[Route("api/exercises-legacy")] // api/exercises-legacy/...
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // JWT vereist.
public class ExercisesLegacyApiController : ControllerBase
{
    private readonly AppDbContext _db; // EF Core DbContext.

    public ExercisesLegacyApiController(AppDbContext db) => _db = db; // DI.

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!; // OwnerId uit JWT.

    [HttpGet] // GET: alle oefeningen van owner, inclusief soft-deleted.
    public async Task<IActionResult> GetAllIncludingDeleted()
    {
        var items = await _db.Exercises // Owner filter, geen IsDeleted filter.
            .Where(e => e.OwnerId == UserId)
            .OrderBy(e => e.Name)
            .Select(e => new // Kleine projection voor debug/legacy gebruik.
            {
                e.Id,
                e.Name,
                e.Category,
                e.Notes,
                e.IsDeleted
            })
            .ToListAsync();

        return Ok(items); // 200 + list.
    }
}
