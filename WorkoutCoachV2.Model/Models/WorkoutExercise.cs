namespace WorkoutCoachV2.Model.Models
{
    public class WorkoutExercise
    {
        public int WorkoutId { get; set; }
        public Workout Workout { get; set; } = default!;

        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; } = default!;

        public int Reps { get; set; } = 5;
    }
}
