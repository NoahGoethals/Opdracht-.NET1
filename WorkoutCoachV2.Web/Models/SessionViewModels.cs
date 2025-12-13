using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Web.Models
{
    public class SessionWorkoutRowViewModel
    {
        public int WorkoutId { get; set; }
        public string WorkoutTitle { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    public class SessionCreateFromWorkoutsViewModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string? Description { get; set; }

        public List<SessionWorkoutRowViewModel> Workouts { get; set; } = new();
    }

    public class SessionEditViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string? Description { get; set; }

        public List<SessionWorkoutRowViewModel> Workouts { get; set; } = new();
    }

    public class SessionDetailsSetRowViewModel
    {
        public int SetNumber { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Reps { get; set; }
        public double Weight { get; set; }
    }

    public class SessionDetailsWorkoutGroupViewModel
    {
        public string WorkoutTitle { get; set; } = string.Empty;
        public List<SessionDetailsSetRowViewModel> Sets { get; set; } = new();
    }

    public class SessionDetailsViewModel
    {
        public int SessionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string? Description { get; set; }

        public List<SessionDetailsWorkoutGroupViewModel> Workouts { get; set; } = new();
        public List<SessionDetailsSetRowViewModel> ExtraSets { get; set; } = new();
    }
}
