// DB-seed: migrate DB, maak rollen/users, seed demo-data (oefeningen/workout), ruim lege sessies op.

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
        // Hou rol-namen consistent overal in je project
        private const string RoleAdmin = "Admin";
        private const string RoleModerator = "Moderator";
        private const string RoleUser = "User";

        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1) Zorg dat DB + migraties up-to-date zijn
            await ctx.Database.MigrateAsync();

            // 2) Rollen aanmaken indien nodig
            await EnsureRoleAsync(roleManager, RoleAdmin);
            await EnsureRoleAsync(roleManager, RoleModerator);
            await EnsureRoleAsync(roleManager, RoleUser);

            // 3) Standaard users + rollen koppelen (idempotent)
            var admin = await EnsureUserAsync(userManager, "admin@local", "Administrator", "Admin!123");
            await EnsureUserInRoleAsync(userManager, admin, RoleAdmin);

            var moderator = await EnsureUserAsync(userManager, "moderator@local", "Moderator", "Moderator!123");
            await EnsureUserInRoleAsync(userManager, moderator, RoleModerator);

            var user = await EnsureUserAsync(userManager, "user@local", "User", "User!123");
            await EnsureUserInRoleAsync(userManager, user, RoleUser);

            // 4) Demo-data (oefeningen/workout) + cleanup (idempotent)
            await SeedDemoDataAsync(ctx);
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

        private static async Task SeedDemoDataAsync(AppDbContext ctx)
        {
            var exercises = ctx.Set<Exercise>();
            var workouts = ctx.Set<Workout>();
            var workoutExercises = ctx.Set<WorkoutExercise>();

            // Basisoefeningen (eenmalig)
            if (!await exercises.AnyAsync())
            {
                exercises.AddRange(
                    new Exercise { Name = "Bench Press" },
                    new Exercise { Name = "Back Squat" },
                    new Exercise { Name = "Deadlift" }
                );
                await ctx.SaveChangesAsync();
            }

            // Minstens één workout voorzien
            Workout workout;
            if (!await workouts.AnyAsync())
            {
                workout = new Workout { Title = "Full Body A" };
                workouts.Add(workout);
                await ctx.SaveChangesAsync();
            }
            else
            {
                workout = await workouts.FirstAsync();
            }

            // Koppel de eerste oefening aan de workout (idempotent)
            var anyLinkForWorkout = await workoutExercises.AnyAsync(we =>
                EF.Property<int>(we, "WorkoutId") == workout.Id);

            if (!anyLinkForWorkout)
            {
                var firstExercise = await exercises.FirstAsync();
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

            // Lege/naamloze sessies opschonen (incl. verweesde sets)
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
