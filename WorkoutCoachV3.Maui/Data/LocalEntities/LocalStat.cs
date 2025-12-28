namespace WorkoutCoachV3.Maui.Data.LocalEntities;

// Lokale "snapshot" / stat record, bedoeld om prestaties per set te bewaren (bv. voor grafieken of history).
public class LocalStat : BaseLocalEntity
{
    // Server-id van de sessie (null zolang er geen remote sessie bestaat of niet gekoppeld is).
    public int? SessionRemoteId { get; set; }
    // Server-id van de oefening (handig voor aggregaties over dezelfde oefening).
    public int? ExerciseRemoteId { get; set; }

    // Kernwaarden van de stat: reps + gewicht + setnummer.
    public int Reps { get; set; }
    public double Weight { get; set; }
    public int SetNumber { get; set; }
}
