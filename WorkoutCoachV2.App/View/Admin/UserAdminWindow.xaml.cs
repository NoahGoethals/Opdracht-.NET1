// File: WorkoutCoachV2.App/View/UserAdminWindow.xaml.cs
// UserAdminWindow: eenvoudig gebruikersbeheer (DisplayName, blokkeren) en rolwissel (Admin/User).

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App.View
{
    public partial class UserAdminWindow : Window
    {
        // Services voor users/rollen en directe DB-toegang (DisplayName/IsBlocked updaten).
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;
        private readonly AppDbContext _db;

        // Grid-bron: één rij per gebruiker.
        public ObservableCollection<Row> Rows { get; } = new();

        // Beschikbare rollen voor de ComboBox.
        public string[] Roles { get; } = new[] { "Admin", "User" };

        // Constructor: dependency-injectie + DataContext instellen en laden.
        public UserAdminWindow(
            UserManager<ApplicationUser> userMgr,
            RoleManager<IdentityRole> roleMgr,
            AppDbContext db)
        {
            InitializeComponent();
            _userMgr = userMgr;
            _roleMgr = roleMgr;
            _db = db;

            DataContext = this;
            _ = LoadAsync();
        }

        // Laadt alle gebruikers en projecteert naar Row (incl. huidige rol).
        private async Task LoadAsync()
        {
            Rows.Clear();

            var users = await _userMgr.Users
                .AsNoTracking()
                .OrderBy(u => u.UserName)
                .ToListAsync();

            foreach (var u in users)
            {
                var roles = await _userMgr.GetRolesAsync(u);
                var role = roles.Contains("Admin") ? "Admin" : "User";

                Rows.Add(new Row
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    DisplayName = u.DisplayName ?? "",
                    IsBlocked = u.IsBlocked,
                    Role = role
                });
            }
        }

        // Vernieuwen-knop: herlaadt de tabel.
        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kon niet vernieuwen:\n{ex.Message}", "Gebruikersbeheer",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Opslaan-knop: rollen garanderen, wijzigingen (DisplayName/IsBlocked/Role) wegschrijven.
        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Zorg dat de twee rollen bestaan.
                foreach (var r in Roles)
                    if (!await _roleMgr.RoleExistsAsync(r))
                        await _roleMgr.CreateAsync(new IdentityRole(r));

                // Voor elke rij: velden updaten en rol aanpassen.
                foreach (var r in Rows)
                {
                    var u = await _db.Users.FirstAsync(x => x.Id == r.Id);

                    // Tekstvelden en blokkade opslaan.
                    u.DisplayName = r.DisplayName ?? "";
                    u.IsBlocked = r.IsBlocked;

                    // Rolset ophalen en bijsturen naar gekozen rol.
                    var current = await _userMgr.GetRolesAsync(u);
                    if (r.Role == "Admin" && !current.Contains("Admin"))
                    {
                        if (current.Any())
                            await _userMgr.RemoveFromRolesAsync(u, current);
                        await _userMgr.AddToRoleAsync(u, "Admin");
                    }
                    else if (r.Role == "User" && !current.Contains("User"))
                    {
                        if (current.Any())
                            await _userMgr.RemoveFromRolesAsync(u, current);
                        await _userMgr.AddToRoleAsync(u, "User");
                    }

                    await _userMgr.UpdateAsync(u);
                }

                await _db.SaveChangesAsync();

                MessageBox.Show("Opgeslagen.", "Gebruikersbeheer",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Opslaan mislukt:\n{ex.Message}", "Gebruikersbeheer",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // View-Model voor een rij in het DataGrid.
        public sealed class Row
        {
            public string Id { get; set; } = "";
            public string UserName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public bool IsBlocked { get; set; }
            public string Role { get; set; } = "User";
        }
    }
}
