using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Exercise : BaseEntity
    {
        public string Name { get; set; } = "";
        public string? Category { get; set; }
        public string? Notes { get; set; }

        public ICollection<WorkoutExercise> InWorkouts { get; set; } = new List<WorkoutExercise>();

        public ICollection<SessionSet> SessionSets { get; set; } = new List<SessionSet>();
    }
}
