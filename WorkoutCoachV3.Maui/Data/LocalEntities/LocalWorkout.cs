namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalWorkout : BaseLocalEntity
{
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
}
