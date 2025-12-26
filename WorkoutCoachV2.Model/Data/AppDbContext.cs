using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<Workout> Workouts => Set<Workout>();
        public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SessionSet> SessionSets => Set<SessionSet>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            if (b.Entity<Exercise>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<Exercise>().HasQueryFilter(e => !EF.Property<bool>(e, "IsDeleted"));

            if (b.Entity<Workout>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<Workout>().HasQueryFilter(w => !EF.Property<bool>(w, "IsDeleted"));

            if (b.Entity<Session>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<Session>().HasQueryFilter(s => !EF.Property<bool>(s, "IsDeleted"));

            if (b.Entity<SessionSet>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<SessionSet>().HasQueryFilter(ss => !EF.Property<bool>(ss, "IsDeleted"));

            b.Entity<WorkoutExercise>()
                .HasKey(we => new { we.WorkoutId, we.ExerciseId });

            b.Entity<WorkoutExercise>()
                .HasOne(we => we.Workout)
                .WithMany(w => w.Exercises)
                .HasForeignKey(we => we.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<WorkoutExercise>()
                .HasOne(we => we.Exercise)
                .WithMany(e => e.InWorkouts)
                .HasForeignKey(we => we.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Entity<SessionSet>()
                .HasOne(ss => ss.Session)
                .WithMany(s => s.Sets)
                .HasForeignKey(ss => ss.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.Entity<SessionSet>()
                .HasOne(ss => ss.Exercise)
                .WithMany(e => e.SessionSets)
                .HasForeignKey(ss => ss.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
