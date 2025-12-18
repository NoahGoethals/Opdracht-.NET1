using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/exercises-legacy")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ExercisesLegacyApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExercisesLegacyApiController(AppDbContext db) => _db = db;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;


    [HttpGet]
    public async Task<IActionResult> GetAllIncludingDeleted()
    {
        var items = await _db.Exercises
            .Where(e => e.OwnerId == UserId)
            .OrderBy(e => e.Name)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Category,
                e.Notes,
                e.IsDeleted
            })
            .ToListAsync();

        return Ok(items);
    }
}
