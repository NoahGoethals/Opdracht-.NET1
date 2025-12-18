namespace WorkoutCoachV3.Maui.Data.LocalEntities;

public class LocalStat : BaseLocalEntity
{
    public int? SessionRemoteId { get; set; }
    public int? ExerciseRemoteId { get; set; }

    public int Reps { get; set; }
    public double Weight { get; set; }
    public int SetNumber { get; set; }
}
