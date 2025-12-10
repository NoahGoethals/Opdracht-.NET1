using System.Collections.Generic;

namespace WorkoutCoachV2.Web.Models
{
    public class WorkoutExerciseRowViewModel
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

        public int Reps { get; set; }
        public double WeightKg { get; set; }
        public int Order { get; set; }
    }

    public class WorkoutExercisesEditViewModel
    {
        public int WorkoutId { get; set; }
        public string WorkoutTitle { get; set; } = string.Empty;
        public List<WorkoutExerciseRowViewModel> Items { get; set; } = new();
    }
}
