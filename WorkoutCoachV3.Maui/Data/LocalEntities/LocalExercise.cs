namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalExercise : BaseLocalEntity
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Notes { get; set; }
}
