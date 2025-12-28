namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Koppeltabel tussen LocalWorkout en LocalExercise met extra velden (reps/gewicht) voor het template.
public class LocalWorkoutExercise : BaseLocalEntity
{
    // Verwijst naar de lokale workout waartoe deze oefening behoort.
    public Guid WorkoutLocalId { get; set; }
    // Verwijst naar de lokale oefening die in de workout zit.
    public Guid ExerciseLocalId { get; set; }

    // Template-instellingen: standaard herhalingen en gewicht voor deze oefening in de workout.
    public int Repetitions { get; set; }
    public double WeightKg { get; set; }

    // Navigatieproperties (optioneel) zodat EF relaties kan laden wanneer nodig.
    public LocalWorkout? Workout { get; set; }
    public LocalExercise? Exercise { get; set; }
}
