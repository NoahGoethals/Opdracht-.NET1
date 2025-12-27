using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Web.Models
{
    // 1 workout-keuze in het session create/edit scherm (checkbox + titel).
    public class SessionWorkoutRowViewModel
    {
        public int WorkoutId { get; set; }
        public string WorkoutTitle { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
    }

    // ViewModel voor “sessie aanmaken” waarbij sets kunnen worden overgenomen uit geselecteerde workouts.
    public class SessionCreateFromWorkoutsViewModel
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string? Description { get; set; }

        public List<SessionWorkoutRowViewModel> Workouts { get; set; } = new();
    }

    // ViewModel voor “sessie aanpassen” met dezelfde workout-selectie logica als create.
    public class SessionEditViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string? Description { get; set; }

        public List<SessionWorkoutRowViewModel> Workouts { get; set; } = new();
    }

    // 1 set-regel voor details: setnummer + oefening + reps + gewicht.
    public class SessionDetailsSetRowViewModel
    {
        public int SetNumber { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int Reps { get; set; }
        public double Weight { get; set; }
    }

    // Groepeert sets per workout-naam in de sessie-details (handig als sets uit meerdere workouts komen).
    public class SessionDetailsWorkoutGroupViewModel
    {
        public string WorkoutTitle { get; set; } = string.Empty;
        public List<SessionDetailsSetRowViewModel> Sets { get; set; } = new();
    }

    // ViewModel voor sessie-details: basisinfo + sets gegroepeerd per workout + extra sets die niet uit workouts komen.
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
