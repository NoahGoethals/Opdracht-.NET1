using System;

namespace WorkoutCoachV2.Model.ApiContracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresUtc,
    string UserId,
    string Email,
    string DisplayName,
    string[] Roles
);
