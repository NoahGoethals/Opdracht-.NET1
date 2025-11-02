namespace WorkoutCoachV2.Model.Models;

public class Exercise : BaseEntity
{
    public string Name { get; set; } = "";
    public string? Category { get; set; }
    public string? Description { get; set; }

    public ICollection<WorkoutExercise> WorkoutExercises { get; set; } = new List<WorkoutExercise>();
}
