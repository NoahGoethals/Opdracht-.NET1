// Uitgevoerde set binnen een sessie; koppelt Session × Exercise met reps/gewicht.

namespace WorkoutCoachV2.Model.Models
{
    public class SessionSet : BaseEntity
    {
        // FK naar sessie + navigatie.
        public int SessionId { get; set; }
        public Session Session { get; set; } = default!;

        // FK naar oefening + navigatie.
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        // Volgnummer van de set binnen de sessie (1..n).
        public int SetNumber { get; set; } = 1;

        // Herhalingen en gewicht (kg) voor deze set.
        public int Reps { get; set; }
        public double Weight { get; set; }

        // Optioneel: RPE en notitie.
        public double? Rpe { get; set; }
        public string? Note { get; set; }
    }
}
