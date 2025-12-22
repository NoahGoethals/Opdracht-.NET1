namespace WorkoutCoachV3.Maui.Services;

public interface IAdminApi
{
    Task<List<AdminUserDto>> GetUsersAsync();
    Task ToggleBlockAsync(string userId);
    Task SetRoleAsync(string userId, string role);
}

public record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    bool IsBlocked,
    string[] Roles
);
