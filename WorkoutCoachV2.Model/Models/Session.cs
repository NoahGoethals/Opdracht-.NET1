using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Model.Models
{
    public class Session : BaseEntity
    {
        public string Title { get; set; } = "";

        public DateTime Date { get; set; } = DateTime.Today;

        public string? Description { get; set; }

        public ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
    }
}
