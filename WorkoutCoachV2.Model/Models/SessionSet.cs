namespace WorkoutCoachV2.Model.Models
{
    public class SessionSet : BaseEntity
    {
        public int SessionId { get; set; }
        public Session Session { get; set; } = default!;

        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        public int SetNumber { get; set; } = 1;
        public int Reps { get; set; }
        public double Weight { get; set; }  
        public double? Rpe { get; set; }   
        public string? Note { get; set; }
    }
}
