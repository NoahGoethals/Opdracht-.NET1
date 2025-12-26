
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data.Seed
{
    public static class DbSeeder
    {
        private const string RoleAdmin = "Admin";
        private const string RoleModerator = "Moderator";
        private const string RoleUser = "User";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await ctx.Database.MigrateAsync();

            await EnsureRoleAsync(roleManager, RoleAdmin);
            await EnsureRoleAsync(roleManager, RoleModerator);
            await EnsureRoleAsync(roleManager, RoleUser);

            var admin = await EnsureUserAsync(userManager, "admin@local", "Administrator", "Admin!123");
            await EnsureUserInRoleAsync(userManager, admin, RoleAdmin);

            var moderator = await EnsureUserAsync(userManager, "moderator@local", "Moderator", "Moderator!123");
            await EnsureUserInRoleAsync(userManager, moderator, RoleModerator);

            var user = await EnsureUserAsync(userManager, "user@local", "User", "User!123");
            await EnsureUserInRoleAsync(userManager, user, RoleUser);

        
            await SeedDemoDataForOwnerAsync(ctx, admin.Id);
            await CleanupUntitledSessionsAsync(ctx);
        }

        private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    var msg = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"DbSeeder: could not create role '{roleName}': {msg}");
                }
            }
        }

        private static async Task<ApplicationUser> EnsureUserAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string displayName,
            string password)
        {
            var user = await userManager.FindByNameAsync(email);
            if (user != null) return user;

            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                DisplayName = displayName,
                IsBlocked = false
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var msg = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"DbSeeder: could not create user '{email}': {msg}");
            }

            return user;
        }

        private static async Task EnsureUserInRoleAsync(
            UserManager<ApplicationUser> userManager,
            ApplicationUser user,
            string roleName)
        {
            if (await userManager.IsInRoleAsync(user, roleName)) return;

            var addResult = await userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
            {
                var msg = string.Join("; ", addResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException(
                    $"DbSeeder: could not add user '{user.UserName}' to role '{roleName}': {msg}");
            }
        }

        private static async Task SeedDemoDataForOwnerAsync(AppDbContext ctx, string ownerId)
        {
            var exercises = ctx.Set<Exercise>();
            var workouts = ctx.Set<Workout>();
            var workoutExercises = ctx.Set<WorkoutExercise>();

            var demoExercises = new[]
            {
                new { Name = "Bench Press", Category = "Strength", Notes = (string?)"Compound chest movement" },
                new { Name = "Back Squat",  Category = "Strength", Notes = (string?)"Compound leg movement" },
                new { Name = "Deadlift",    Category = "Strength", Notes = (string?)"Posterior chain compound lift" }
            };

            foreach (var ex in demoExercises)
            {
                var exists = await exercises.AnyAsync(e =>
                    e.OwnerId == ownerId &&
                    e.Name == ex.Name);

                if (!exists)
                {
                    exercises.Add(new Exercise
                    {
                        Name = ex.Name,
                        Category = ex.Category,
                        Notes = ex.Notes,
                        OwnerId = ownerId
                    });
                }
            }

            await ctx.SaveChangesAsync();

            const string workoutTitle = "Full Body A";

            var workout = await workouts.FirstOrDefaultAsync(w =>
                w.OwnerId == ownerId &&
                w.Title == workoutTitle);

            if (workout == null)
            {
                workout = new Workout
                {
                    Title = workoutTitle,
                    OwnerId = ownerId
                };

                workouts.Add(workout);
                await ctx.SaveChangesAsync();
            }

            var anyLinkForWorkout = await workoutExercises.AnyAsync(we => we.WorkoutId == workout.Id);

            if (!anyLinkForWorkout)
            {
                var firstExercise = await exercises
                    .Where(e => e.OwnerId == ownerId)
                    .OrderBy(e => e.Name)
                    .FirstAsync();

                workoutExercises.Add(new WorkoutExercise
                {
                    WorkoutId = workout.Id,
                    ExerciseId = firstExercise.Id,
                    Reps = 5,
                    WeightKg = 0
                });

                await ctx.SaveChangesAsync();
            }
        }

        private static async Task CleanupUntitledSessionsAsync(AppDbContext ctx)
        {
            var sessions = ctx.Set<Session>();
            var sessionSets = ctx.Set<SessionSet>();

            var untitledIds = await sessions
                .Where(s => string.IsNullOrWhiteSpace(s.Title))
                .Select(s => s.Id)
                .ToListAsync();

            if (untitledIds.Count == 0) return;

            var orphanSets = await sessionSets
                .Where(ss => untitledIds.Contains(ss.SessionId))
                .ToListAsync();

            if (orphanSets.Count > 0)
                sessionSets.RemoveRange(orphanSets);

            var toRemove = await sessions
                .Where(s => untitledIds.Contains(s.Id))
                .ToListAsync();

            sessions.RemoveRange(toRemove);

            await ctx.SaveChangesAsync();
        }
    }
}
