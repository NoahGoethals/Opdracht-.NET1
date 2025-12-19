using Microsoft.EntityFrameworkCore;
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Data;

public class LocalAppDbContext : DbContext
{
    public LocalAppDbContext(DbContextOptions<LocalAppDbContext> options) : base(options) { }

    public DbSet<LocalExercise> Exercises => Set<LocalExercise>();
    public DbSet<LocalWorkout> Workouts => Set<LocalWorkout>();
    public DbSet<LocalWorkoutExercise> WorkoutExercises => Set<LocalWorkoutExercise>();

    public DbSet<LocalSession> Sessions => Set<LocalSession>();
    public DbSet<LocalSessionSet> SessionSets => Set<LocalSessionSet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LocalExercise>().HasIndex(x => x.RemoteId);
        modelBuilder.Entity<LocalWorkout>().HasIndex(x => x.RemoteId);
        modelBuilder.Entity<LocalSession>().HasIndex(x => x.RemoteId);
        modelBuilder.Entity<LocalSessionSet>().HasIndex(x => x.RemoteId);

        modelBuilder.Entity<LocalWorkoutExercise>(b =>
        {
            b.HasIndex(x => new { x.WorkoutLocalId, x.ExerciseLocalId }).IsUnique();

            b.HasOne(x => x.Workout)
                .WithMany()
                .HasForeignKey(x => x.WorkoutLocalId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseLocalId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LocalSession>(b =>
        {
            b.Property(x => x.Title).IsRequired();
        });

        modelBuilder.Entity<LocalSessionSet>(b =>
        {
            b.HasIndex(x => new { x.SessionLocalId, x.ExerciseLocalId, x.SetNumber })
             .IsUnique();

            b.HasOne<LocalSession>()
                .WithMany()
                .HasForeignKey(x => x.SessionLocalId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne<LocalExercise>()
                .WithMany()
                .HasForeignKey(x => x.ExerciseLocalId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
