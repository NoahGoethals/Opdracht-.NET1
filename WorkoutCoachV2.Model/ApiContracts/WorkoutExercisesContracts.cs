namespace WorkoutCoachV2.Model.ApiContracts;

public sealed record WorkoutExerciseDto(int ExerciseId, string ExerciseName, int Reps, double? WeightKg);

public sealed record UpsertWorkoutExerciseDto(int ExerciseId, int Reps, double? WeightKg);
