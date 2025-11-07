using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        private readonly UserManager<ApplicationUser> _userMgr;
        private readonly RoleManager<IdentityRole> _roleMgr;
        private readonly AppDbContext _db;

        public ObservableCollection<Row> Rows { get; } = new();
        public string[] Roles { get; private set; } = new[] { "Admin", "User" };

        public UserAdminWindow(UserManager<ApplicationUser> userMgr, RoleManager<IdentityRole> roleMgr, AppDbContext db)
        {
            InitializeComponent();
            _userMgr = userMgr;
            _roleMgr = roleMgr;
            _db = db;

            dg.ItemsSource = Rows;
            Loaded += async (_, __) => await LoadAsync();
        }

        private async Task LoadAsync()
        {
            Rows.Clear();
            var users = await _db.Users.AsNoTracking().ToListAsync();
            foreach (var u in users)
            {
                var roles = await _userMgr.GetRolesAsync(u);
                Rows.Add(new Row
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    DisplayName = u.DisplayName,
                    IsBlocked = u.IsBlocked,
                    Role = roles.FirstOrDefault() ?? "User"
                });
            }
            dg.Items.Refresh();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            foreach (var r in Rows)
            {
                var u = await _userMgr.FindByIdAsync(r.Id);
                if (u == null) continue;
                u.DisplayName = r.DisplayName;
                u.IsBlocked = r.IsBlocked;
                await _userMgr.UpdateAsync(u);

                var current = await _userMgr.GetRolesAsync(u);
                foreach (var c in current) await _userMgr.RemoveFromRoleAsync(u, c);
                await _userMgr.AddToRoleAsync(u, r.Role);
            }

            MessageBox.Show("Opgeslagen.", "Gebruikersbeheer");
            await LoadAsync();
        }

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
