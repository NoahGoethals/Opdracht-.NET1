using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.ApiContracts;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController] // API voor beheer van workout-oefeningen (koppeltabel).
[Route("api/workouts/{workoutId:int}/exercises")] // Nested route per workout.
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] // JWT vereist.
public class WorkoutExercisesApiController : ControllerBase
{
    private readonly AppDbContext _db; // EF Core DbContext.

    public WorkoutExercisesApiController(AppDbContext db) => _db = db; // DI.

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier); // OwnerId uit JWT.

    [HttpGet] // GET: alle oefeningen gekoppeld aan workout (incl. reps/weight).
    public async Task<ActionResult<List<WorkoutExerciseDto>>> GetAll(int workoutId, CancellationToken ct)
    {
        var uid = UserId;
        if (uid is null) return Unauthorized(); // 401.

        var workout = await _db.Workouts // Workout ownership check.
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.OwnerId == uid && !w.IsDeleted, ct);

        if (workout is null) return NotFound(); // 404.

        var rows = await _db.WorkoutExercises // Koppelingen laden + Exercise includen voor naam.
            .AsNoTracking()
            .Where(we => we.WorkoutId == workoutId)
            .Include(we => we.Exercise)
            .OrderBy(we => we.Exercise.Name)
            .Select(we => new WorkoutExerciseDto(
                we.ExerciseId,
                we.Exercise.Name,
                we.Reps,
                we.WeightKg
            ))
            .ToListAsync(ct);

        return rows; // 200 + list (implicit Ok).
    }

    [HttpPut] // PUT: vervangt alle workout-exercises in één call (sync-friendly).
    public async Task<IActionResult> ReplaceAll(int workoutId, [FromBody] List<UpsertWorkoutExerciseDto> items, CancellationToken ct)
    {
        var uid = UserId;
        if (uid is null) return Unauthorized(); // 401.

        var workout = await _db.Workouts // Workout ownership check.
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.OwnerId == uid && !w.IsDeleted, ct);

        if (workout is null) return NotFound(); // 404.

        items ??= new(); // Null-safe.

        var normalized = items // Duplicaten op ExerciseId samenvoegen (laatste wint).
            .GroupBy(x => x.ExerciseId)
            .Select(g => g.Last())
            .ToList();

        var exerciseIds = normalized.Select(x => x.ExerciseId).Distinct().ToList(); // Unieke ids.

        var allowedExerciseIds = await _db.Exercises // Alleen owner + niet deleted oefeningen toelaten.
            .AsNoTracking()
            .Where(e => exerciseIds.Contains(e.Id) && e.OwnerId == uid && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var allowedSet = allowedExerciseIds.ToHashSet(); // Snelle contains-check.

        var existing = await _db.WorkoutExercises // Bestaande koppelingen ophalen.
            .Where(we => we.WorkoutId == workoutId)
            .ToListAsync(ct);

        _db.WorkoutExercises.RemoveRange(existing); // Volledige replace.

        foreach (var row in normalized) // Nieuwe koppelingen toevoegen.
        {
            if (!allowedSet.Contains(row.ExerciseId)) // On-toegelaten exercises overslaan.
                continue;

            _db.WorkoutExercises.Add(new WorkoutExercise
            {
                WorkoutId = workoutId,
                ExerciseId = row.ExerciseId,
                Reps = Math.Max(0, row.Reps),
                WeightKg = row.WeightKg is null ? null : Math.Max(0, row.WeightKg.Value)
            });
        }

        await _db.SaveChangesAsync(ct); // Persist.
        return NoContent(); // 204.
    }
}
