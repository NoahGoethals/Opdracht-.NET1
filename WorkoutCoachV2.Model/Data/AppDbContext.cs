// EF Core DbContext + Identity: DbSets, soft-delete filters (indien aanwezig), relaties & cascade-regels.

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        // Tabellen
        public DbSet<Exercise> Exercises => Set<Exercise>();
        public DbSet<Workout> Workouts => Set<Workout>();
        public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
        public DbSet<Session> Sessions => Set<Session>();
        public DbSet<SessionSet> SessionSets => Set<SessionSet>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // Soft-delete filters enkel toepassen als property bestaat
            if (b.Entity<Exercise>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<Exercise>().HasQueryFilter(e => EF.Property<bool>(e, "IsDeleted") == false);
            if (b.Entity<Workout>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<Workout>().HasQueryFilter(w => EF.Property<bool>(w, "IsDeleted") == false);
            if (b.Entity<Session>().Metadata.FindProperty("IsDeleted") != null)
                b.Entity<Session>().HasQueryFilter(s => EF.Property<bool>(s, "IsDeleted") == false);

            // WorkoutExercise: samengestelde PK (WorkoutId + ExerciseId)
            b.Entity<WorkoutExercise>()
                .HasKey(we => new { we.WorkoutId, we.ExerciseId });

            // Relatie: WorkoutExercise -> Workout (many-to-one), cascade bij verwijderen Workout
            b.Entity<WorkoutExercise>()
                .HasOne(we => we.Workout)
                .WithMany() // geen navigatie-collectie op Workout
                .HasForeignKey(we => we.WorkoutId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relatie: WorkoutExercise -> Exercise (many-to-one), restrict bij verwijderen Exercise
            b.Entity<WorkoutExercise>()
                .HasOne(we => we.Exercise)
                .WithMany() // geen navigatie-collectie op Exercise
                .HasForeignKey(we => we.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relatie: SessionSet -> Session (many-to-one), cascade bij verwijderen Session
            b.Entity<SessionSet>()
                .HasOne(ss => ss.Session)
                .WithMany(s => s.Sets)
                .HasForeignKey(ss => ss.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Relatie: SessionSet -> Exercise (many-to-one), restrict bij verwijderen Exercise
            b.Entity<SessionSet>()
                .HasOne(ss => ss.Exercise)
                .WithMany()
                .HasForeignKey(ss => ss.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
