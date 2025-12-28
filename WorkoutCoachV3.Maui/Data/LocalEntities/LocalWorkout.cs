namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Lokale workout-template (bv. "Push day") die offline kan bestaan en later gesynct wordt.
public class LocalWorkout : BaseLocalEntity
{
    // Titel van de workout (naam zichtbaar in lijsten).
    public string Title { get; set; } = "";
    // Optionele notities bij de workout (bv. tips, targets, uitleg).
    public string? Notes { get; set; }
}
