using CommunityToolkit.Mvvm.ComponentModel;

namespace WorkoutCoachV3.Maui.ViewModels;

public partial class AdminUserRow : ObservableObject
{
    // Immutable basis info voor display in de lijst.
    public string Id { get; }
    public string Email { get; }
    public string DisplayName { get; }

    // UI-editable velden: block status + gekozen role (Picker binding).
    [ObservableProperty] private bool isBlocked;
    [ObservableProperty] private string selectedRole;

    public AdminUserRow(string id, string email, string displayName, bool isBlocked, string selectedRole)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        IsBlocked = isBlocked;
        SelectedRole = selectedRole;
    }
}
