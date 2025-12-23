using System;
using System.Collections.Generic;

namespace WorkoutCoachV2.Model.ApiContracts;

public sealed record SessionSetDto(int ExerciseId, int SetNumber, int Reps, double Weight);

public sealed record SessionDto(
    int Id,
    string Title,
    DateTime Date,
    string? Description,
    int SetsCount,
    List<SessionSetDto> Sets
);

public sealed record UpsertSessionDto(
    string Title,
    DateTime Date,
    string? Description,
    List<SessionSetDto> Sets
);
