// Represent a performed set inside a session; link Session × Exercise with reps/weight.

namespace WorkoutCoachV2.Model.Models
{
    public class SessionSet : BaseEntity
    {
        // Reference the parent session.
        public int SessionId { get; set; }
        public Session Session { get; set; } = default!;

        // Reference the performed exercise.
        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        // Order this set within the session (1..n).
        public int SetNumber { get; set; } = 1;

        // Store performance metrics.
        public int Reps { get; set; }
        public double Weight { get; set; }

        // Capture optional intensity and notes.
        public double? Rpe { get; set; }
        public string? Note { get; set; }
    }
}
