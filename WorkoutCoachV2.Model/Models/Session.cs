using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Session : BaseEntity
    {
        public DateTime PerformedOn { get; set; } = DateTime.Today;
        public string? Notes { get; set; }

        public int? WorkoutId { get; set; }
        public Workout? Workout { get; set; }

        public ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
    }
}
