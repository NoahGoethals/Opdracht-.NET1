using System;

namespace WorkoutCoachV2.Model.ApiContracts;

public sealed record WorkoutDto(int Id, string Title, DateTime? ScheduledOn);

public sealed record CreateWorkoutDto(string Title, DateTime? ScheduledOn);

public sealed record UpdateWorkoutDto(string Title, DateTime? ScheduledOn);
