using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace WorkoutCoachV2.Web.Models
{
    public class StatsIndexViewModel
    {
        public int? ExerciseId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public SelectListItem[] Exercises { get; set; } = Array.Empty<SelectListItem>();

        public StatsResultsViewModel? Results { get; set; }
    }

    public class StatsResultsViewModel
    {
        public int? ExerciseId { get; set; }
        public string? ExerciseName { get; set; }

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public int SessionsCount { get; set; }
        public int SetsCount { get; set; }
        public int TotalReps { get; set; }
        public double TotalVolumeKg { get; set; }

        public double MaxWeight { get; set; }
        public double BestEstimated1Rm { get; set; }

        public List<StatsSessionRowViewModel> PerSession { get; set; } = new();

        public List<StatsTopExerciseRowViewModel> TopExercises { get; set; } = new();
    }

    public class StatsSessionRowViewModel
    {
        public DateTime Date { get; set; }
        public string SessionTitle { get; set; } = string.Empty;

        public int SetsCount { get; set; }
        public int TotalReps { get; set; }
        public double TotalVolumeKg { get; set; }
        public double MaxWeight { get; set; }
    }

    public class StatsTopExerciseRowViewModel
    {
        public int ExerciseId { get; set; }
        public string ExerciseName { get; set; } = string.Empty;

        public int SetsCount { get; set; }
        public int TotalReps { get; set; }
        public double TotalVolumeKg { get; set; }
        public double MaxWeight { get; set; }
    }
}
