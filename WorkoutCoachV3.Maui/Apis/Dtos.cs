namespace WorkoutCoachV3.Maui.Apis;

public sealed record ExerciseDto(int Id, string Name, string? Category, string? Notes);
public sealed record WorkoutDto(int Id, string Title);

public sealed record CreateExerciseDto(string Name, string? Category, string? Notes);
public sealed record UpdateExerciseDto(string Name, string? Category, string? Notes);

public sealed record CreateWorkoutDto(string Title, DateTime? ScheduledOn);
public sealed record UpdateWorkoutDto(string Title, DateTime? ScheduledOn);
