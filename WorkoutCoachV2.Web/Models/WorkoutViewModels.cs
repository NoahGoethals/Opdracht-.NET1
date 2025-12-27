using System.Collections.Generic;

namespace WorkoutCoachV2.Web.Models
{
    // 1 rij in het “workout oefeningen aanpassen” scherm: checkbox + reps/gewicht per oefening.
    public class WorkoutExerciseRowViewModel
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

        public int Reps { get; set; }
        public double WeightKg { get; set; }
        public int Order { get; set; }
    }

    // ViewModel voor het aanpassen van oefeningen in een workout: titel + lijst met alle oefeningen als rows.
    public class WorkoutExercisesEditViewModel
    {
        public int WorkoutId { get; set; }
        public string WorkoutTitle { get; set; } = string.Empty;
        public List<WorkoutExerciseRowViewModel> Items { get; set; } = new();
    }
}
