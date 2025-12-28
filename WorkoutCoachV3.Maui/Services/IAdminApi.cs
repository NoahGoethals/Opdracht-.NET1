// Admin contract: endpoints voor user beheer + DTO record.
namespace WorkoutCoachV3.Maui.Services;

public interface IAdminApi
{
    // Haalt alle users op (voor AdminPanelPage).
    Task<List<AdminUserDto>> GetUsersAsync();
    // Toggle block/unblock via userId.
    Task ToggleBlockAsync(string userId);
    // Zet rol voor user (bv. "Admin", "User").
    Task SetRoleAsync(string userId, string role);
}

// DTO voor admin user rij (id, identiteit, block status, rollen).
public record AdminUserDto(
    string Id,
    string Email,
    string DisplayName,
    bool IsBlocked,
    string[] Roles
);
