namespace WorkoutCoachV2.Model.Models;

public class Workout : BaseEntity
{
    public string Title { get; set; } = "";
    public DateTime ScheduledOn { get; set; } = DateTime.Today;
    public string? Notes { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
