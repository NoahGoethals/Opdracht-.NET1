namespace WorkoutCoachV3.Maui.Services;


public record WorkoutExerciseDto(int ExerciseId, string Name, int Repetitions, double WeightKg);

public record SaveWorkoutExerciseDto(int ExerciseId, int Repetitions, double WeightKg);
