using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data.Seed
{
    public class DbSeeder
    {
        private readonly AppDbContext _ctx;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly UserManager<AppUser> _users;

        public DbSeeder(AppDbContext ctx, RoleManager<IdentityRole> roles, UserManager<AppUser> users)
        {
            _ctx = ctx;
            _roles = roles;
            _users = users;
        }

        public async Task SeedAsync()
        {
            await _ctx.Database.MigrateAsync();

            var roleNames = new[] { "Admin", "Coach", "Member" };
            foreach (var r in roleNames)
                if (!await _roles.RoleExistsAsync(r))
                    await _roles.CreateAsync(new IdentityRole(r));

            const string adminEmail = "admin@local";
            var admin = await _users.FindByEmailAsync(adminEmail);
            if (admin is null)
            {
                admin = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Admin"
                };
                await _users.CreateAsync(admin, "Admin!12345");
                await _users.AddToRolesAsync(admin, roleNames);
            }

            if (!await _ctx.Exercises.AnyAsync())
            {
                var squat = new Exercise { Name = "Back Squat", Category = "Legs" };
                var bench = new Exercise { Name = "Bench Press", Category = "Chest" };
                var row = new Exercise { Name = "Barbell Row", Category = "Back" };
                _ctx.Exercises.AddRange(squat, bench, row);

                var w = new Workout { Title = "Starting Strength A", ScheduledOn = DateTime.Today };
                _ctx.Workouts.Add(w);

                _ctx.WorkoutExercises.AddRange(
                    new WorkoutExercise { Workout = w, Exercise = squat, Sets = 5, Reps = 5 },
                    new WorkoutExercise { Workout = w, Exercise = bench, Sets = 5, Reps = 5 },
                    new WorkoutExercise { Workout = w, Exercise = row, Sets = 5, Reps = 5 }
                );

                await _ctx.SaveChangesAsync();
            }
        }
    }
}
