namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Lokale representatie van een oefening (los van server) voor offline gebruik.
public class LocalExercise : BaseLocalEntity
{
    // Naam van de oefening (bv. "Bench Press").
    public string Name { get; set; } = "";
    // Categorie (bv. "Chest", "Legs", "Cardio", ...).
    public string Category { get; set; } = "";
    // Optionele notities/extra info over de oefening.
    public string? Notes { get; set; }
}
