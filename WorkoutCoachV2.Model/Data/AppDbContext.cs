using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<Workout> Workouts => Set<Workout>();
        public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();

        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SessionSet> SessionSets => Set<SessionSet>();

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            b.Entity<WorkoutExercise>()
                .HasKey(x => new { x.WorkoutId, x.ExerciseId });

            b.Entity<WorkoutExercise>()
                .HasOne(x => x.Workout)
                .WithMany(x => x.Exercises)
                .HasForeignKey(x => x.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<WorkoutExercise>()
                .HasOne(x => x.Exercise)
                .WithMany(x => x.InWorkouts)
                .HasForeignKey(x => x.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<SessionSet>()
                .HasOne(s => s.Session)
                .WithMany(s => s.Sets)
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<SessionSet>()
                .HasOne(s => s.Exercise)
                .WithMany(e => e.SessionSets)
                .HasForeignKey(s => s.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<Exercise>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Workout>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<Session>().HasQueryFilter(e => !e.IsDeleted);
            b.Entity<SessionSet>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
