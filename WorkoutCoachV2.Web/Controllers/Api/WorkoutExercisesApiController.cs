using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Web.Controllers.Api;

[ApiController]
[Route("api/workouts/{workoutId:int}/exercises")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkoutExercisesApiController : ControllerBase
{
    private readonly AppDbContext _db;

    public WorkoutExercisesApiController(AppDbContext db)
    {
        _db = db;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public sealed record WorkoutExerciseDto(int ExerciseId, string ExerciseName, int Reps, double? WeightKg);

    public sealed record UpsertWorkoutExerciseDto(int ExerciseId, int Reps, double? WeightKg);

    [HttpGet]
    public async Task<ActionResult<List<WorkoutExerciseDto>>> GetAll(int workoutId, CancellationToken ct)
    {
        var uid = UserId;
        if (uid is null) return Unauthorized();

        var workout = await _db.Workouts
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.OwnerId == uid && !w.IsDeleted, ct);

        if (workout is null) return NotFound();

        var rows = await _db.WorkoutExercises
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

        return rows;
    }

    [HttpPut]
    public async Task<IActionResult> ReplaceAll(int workoutId, [FromBody] List<UpsertWorkoutExerciseDto> items, CancellationToken ct)
    {
        var uid = UserId;
        if (uid is null) return Unauthorized();

        var workout = await _db.Workouts
            .FirstOrDefaultAsync(w => w.Id == workoutId && w.OwnerId == uid && !w.IsDeleted, ct);

        if (workout is null) return NotFound();

        items ??= new();
        var normalized = items
            .GroupBy(x => x.ExerciseId)
            .Select(g => g.Last())
            .ToList();

        var exerciseIds = normalized.Select(x => x.ExerciseId).Distinct().ToList();

        var allowedExerciseIds = await _db.Exercises
            .AsNoTracking()
            .Where(e => exerciseIds.Contains(e.Id) && e.OwnerId == uid && !e.IsDeleted)
            .Select(e => e.Id)
            .ToListAsync(ct);

        var allowedSet = allowedExerciseIds.ToHashSet();

        var existing = await _db.WorkoutExercises
            .Where(we => we.WorkoutId == workoutId)
            .ToListAsync(ct);

        _db.WorkoutExercises.RemoveRange(existing);

        foreach (var row in normalized)
        {
            if (!allowedSet.Contains(row.ExerciseId))
                continue;

            _db.WorkoutExercises.Add(new WorkoutExercise
            {
                WorkoutId = workoutId,
                ExerciseId = row.ExerciseId,
                Reps = Math.Max(0, row.Reps),
                WeightKg = row.WeightKg is null ? null : Math.Max(0, row.WeightKg.Value)
            });
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
