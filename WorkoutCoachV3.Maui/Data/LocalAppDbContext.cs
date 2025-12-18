using Microsoft.EntityFrameworkCore;
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Data;

public class LocalAppDbContext : DbContext
{
    public LocalAppDbContext(DbContextOptions<LocalAppDbContext> options) : base(options) { }

    public DbSet<LocalExercise> Exercises => Set<LocalExercise>();
    public DbSet<LocalWorkout> Workouts => Set<LocalWorkout>();
    public DbSet<LocalWorkoutExercise> WorkoutExercises => Set<LocalWorkoutExercise>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocalExercise>()
            .HasIndex(x => x.RemoteId);

        modelBuilder.Entity<LocalWorkout>()
            .HasIndex(x => x.RemoteId);

        modelBuilder.Entity<LocalWorkoutExercise>()
            .HasIndex(x => x.RemoteId);

        modelBuilder.Entity<LocalWorkoutExercise>()
            .HasIndex(x => new { x.WorkoutLocalId, x.ExerciseLocalId })
            .IsUnique();
    }
}
