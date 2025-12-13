using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WorkoutCoachV2.Model.Models
{
    public class Session : BaseEntity
    {
        [Required]
        public string Title { get; set; } = "";

        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        public string? Description { get; set; }

        // Per-user data
        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public ICollection<SessionSet> Sets { get; set; } = new List<SessionSet>();
    }
}
