namespace WorkoutCoachV2.Model.ApiContracts;

public sealed record ExerciseDto(int Id, string Name, string? Category, string? Notes);

public sealed record CreateExerciseDto(string Name, string? Category, string? Notes);

public sealed record UpdateExerciseDto(string Name, string? Category, string? Notes);
