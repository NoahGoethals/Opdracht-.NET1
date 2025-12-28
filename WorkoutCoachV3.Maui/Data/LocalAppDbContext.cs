// EF Core namespace voor DbContext, DbSet, ModelBuilder en configuratie via Fluent API.
using Microsoft.EntityFrameworkCore;
// Lokale entity-klassen (SQLite/offline) die door deze DbContext gemapt worden.
using WorkoutCoachV3.Maui.Data.LocalEntities;

namespace WorkoutCoachV3.Maui.Data;

// DbContext voor de lokale database (meestal SQLite) van de MAUI-app.
public class LocalAppDbContext : DbContext
{
    // Constructor die DbContextOptions ontvangt via DI (configuratie zoals SQLite pad, logging, etc.).
    public LocalAppDbContext(DbContextOptions<LocalAppDbContext> options) : base(options) { }

    // Tabel voor oefeningen (offline opslag + sync velden).
    public DbSet<LocalExercise> Exercises => Set<LocalExercise>();
    // Tabel voor workout templates.
    public DbSet<LocalWorkout> Workouts => Set<LocalWorkout>();
    // Koppeltabel tussen workouts en oefeningen (met reps/gewicht template data).
    public DbSet<LocalWorkoutExercise> WorkoutExercises => Set<LocalWorkoutExercise>();

    // Tabel voor gelogde sessies (workouts die effectief uitgevoerd werden).
    public DbSet<LocalSession> Sessions => Set<LocalSession>();
    // Tabel voor sets binnen een sessie.
    public DbSet<LocalSessionSet> SessionSets => Set<LocalSessionSet>();

    // Fluent API configuratie voor indexes, unique constraints en relaties.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Laat EF Core eerst de basisconfig uitvoeren (conventies, DataAnnotations, etc.).
        base.OnModelCreating(modelBuilder);

        // Indexen op RemoteId versnellen sync-lookups (lokale record <-> server record).
        modelBuilder.Entity<LocalExercise>().HasIndex(x => x.RemoteId);
        modelBuilder.Entity<LocalWorkout>().HasIndex(x => x.RemoteId);
        modelBuilder.Entity<LocalSession>().HasIndex(x => x.RemoteId);
        modelBuilder.Entity<LocalSessionSet>().HasIndex(x => x.RemoteId);

        // Configuratie voor de workout-oefening koppelingstabel.
        modelBuilder.Entity<LocalWorkoutExercise>(b =>
        {
            // Unieke combinatie zodat dezelfde oefening niet dubbel in dezelfde workout zit.
            b.HasIndex(x => new { x.WorkoutLocalId, x.ExerciseLocalId }).IsUnique();

            // Relatie: LocalWorkoutExercise -> LocalWorkout via WorkoutLocalId.
            b.HasOne(x => x.Workout)
                // Geen expliciete collection property in LocalWorkout, dus WithMany() zonder parameter.
                .WithMany()
                // Foreign key is de lokale Guid van de workout.
                .HasForeignKey(x => x.WorkoutLocalId)
                // Cascade delete: verwijder je een workout, dan verdwijnen de gekoppelde records mee.
                .OnDelete(DeleteBehavior.Cascade);

            // Relatie: LocalWorkoutExercise -> LocalExercise via ExerciseLocalId.
            b.HasOne(x => x.Exercise)
                .WithMany()
                .HasForeignKey(x => x.ExerciseLocalId)
                // Restrict: oefening mag niet "per ongeluk" mee verwijderd worden door templates.
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Extra constraint op LocalSession (naast eventueel [Required] in entity).
        modelBuilder.Entity<LocalSession>(b =>
        {
            // Title is verplicht in de database (NOT NULL).
            b.Property(x => x.Title).IsRequired();
        });

        // Configuratie voor LocalSessionSet (sets binnen een sessie).
        modelBuilder.Entity<LocalSessionSet>(b =>
        {
            // Uniek per sessie + oefening + setnummer zodat je geen dubbele set 1,2,3 krijgt.
            b.HasIndex(x => new { x.SessionLocalId, x.ExerciseLocalId, x.SetNumber })
             .IsUnique();

            // Relatie: SessionSet hoort bij één LocalSession (via SessionLocalId).
            b.HasOne<LocalSession>()
                .WithMany()
                .HasForeignKey(x => x.SessionLocalId)
                // Als sessie verdwijnt, mogen de sets ook mee weg.
                .OnDelete(DeleteBehavior.Cascade);

            // Relatie: SessionSet verwijst naar één LocalExercise (via ExerciseLocalId).
            b.HasOne<LocalExercise>()
                .WithMany()
                .HasForeignKey(x => x.ExerciseLocalId)
                // Restrict om onbedoeld verwijderen van oefeningen te voorkomen.
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
