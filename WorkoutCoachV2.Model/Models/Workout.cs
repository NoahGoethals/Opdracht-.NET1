using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Workout : BaseEntity
    {
        public string Title { get; set; } = "";
        public DateTime? ScheduledOn { get; set; }

        public ICollection<WorkoutExercise> Exercises { get; set; } = new List<WorkoutExercise>();

        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
