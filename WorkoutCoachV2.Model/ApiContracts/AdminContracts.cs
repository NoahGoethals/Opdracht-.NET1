namespace WorkoutCoachV2.Model.ApiContracts;

public sealed record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    bool IsBlocked,
    string[] Roles
);

public sealed record SetRoleRequest(string Role);
