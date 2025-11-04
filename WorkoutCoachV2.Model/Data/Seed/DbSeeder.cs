using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data.Seed
{
    public class DbSeeder
    {
        private readonly AppDbContext _ctx;
        private readonly RoleManager<IdentityRole> _roles;
        private readonly UserManager<AppUser> _users;

        private static readonly string[] DefaultRoles = { "Admin", "Coach", "Member" };
        private const string AdminEmail = "admin@local";
        private const string AdminPassword = "Admin!12345";

        public DbSeeder(AppDbContext ctx, RoleManager<IdentityRole> roles, UserManager<AppUser> users)
        {
            _ctx = ctx;
            _roles = roles;
            _users = users;
        }

        public async Task SeedAsync()
        {
            await _ctx.Database.MigrateAsync();

            foreach (var r in DefaultRoles)
            {
                if (!await _roles.RoleExistsAsync(r))
                    await _roles.CreateAsync(new IdentityRole(r));
            }

            var admin = await _users.FindByEmailAsync(AdminEmail);
            if (admin is null)
            {
                admin = new AppUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Admin"
                };
                await _users.CreateAsync(admin, AdminPassword);
                await _users.AddToRolesAsync(admin, DefaultRoles);
            }
            else
            {
                var current = await _users.GetRolesAsync(admin);
                var missing = DefaultRoles.Except(current).ToArray();
                if (missing.Length > 0)
                    await _users.AddToRolesAsync(admin, missing);
            }

            var squat = await FindOrCreateExerciseAsync("Back Squat", "Legs");
            var bench = await FindOrCreateExerciseAsync("Bench Press", "Chest");
            var row = await FindOrCreateExerciseAsync("Barbell Row", "Back");

            var workout = await _ctx.Workouts
                .Include(w => w.Exercises)
                .FirstOrDefaultAsync(w => w.Title == "Starting Strength A");

            if (workout is null)
            {
                workout = new Workout
                {
                    Title = "Starting Strength A",
                    ScheduledOn = DateTime.Today
                };
                _ctx.Workouts.Add(workout);
                await _ctx.SaveChangesAsync();

                _ctx.WorkoutExercises.AddRange(
                    new WorkoutExercise { WorkoutId = workout.Id, ExerciseId = squat.Id, Sets = 5, Reps = 5 },
                    new WorkoutExercise { WorkoutId = workout.Id, ExerciseId = bench.Id, Sets = 5, Reps = 5 },
                    new WorkoutExercise { WorkoutId = workout.Id, ExerciseId = row.Id, Sets = 5, Reps = 5 }
                );
                await _ctx.SaveChangesAsync();
            }

            if (!await _ctx.Sessions.AnyAsync())
            {
                var session = new Session
                {
                    Title = "Test session",
                    Date = DateTime.Today
                };
                _ctx.Sessions.Add(session);
                await _ctx.SaveChangesAsync();

                _ctx.SessionSets.AddRange(
                    new SessionSet { SessionId = session.Id, ExerciseId = squat.Id, Reps = 5, Weight = 80 },
                    new SessionSet { SessionId = session.Id, ExerciseId = bench.Id, Reps = 5, Weight = 60 }
                );
                await _ctx.SaveChangesAsync();
            }
        }


        private async Task<Exercise> FindOrCreateExerciseAsync(string name, string? category)
        {
            var ex = await _ctx.Exercises.FirstOrDefaultAsync(e => e.Name == name);
            if (ex is not null) return ex;

            ex = new Exercise { Name = name, Category = category };
            _ctx.Exercises.Add(ex);
            await _ctx.SaveChangesAsync();
            return ex;
        }
    }
}
