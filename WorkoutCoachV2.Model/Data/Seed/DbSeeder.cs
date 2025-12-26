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

            // Zorg dat DB + migraties up-to-date zijn
            await ctx.Database.MigrateAsync();

            // Rollen aanmaken indien nodig
            async Task EnsureRoleAsync(string roleName)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            await EnsureRoleAsync(RoleAdmin);
            await EnsureRoleAsync(RoleModerator);
            await EnsureRoleAsync(RoleUser);

            // Helpers
            async Task<ApplicationUser> EnsureUserAsync(string email, string displayName, string password)
            {
                var user = await userManager.FindByNameAsync(email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        DisplayName = displayName
                    };

                    var createResult = await userManager.CreateAsync(user, password);
                    if (!createResult.Succeeded)
                    {
                        var msg = string.Join("; ", createResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"DbSeeder: could not create user '{email}': {msg}");
                    }
                }

                return user;
            }

            async Task EnsureUserInRoleAsync(ApplicationUser user, string role)
            {
                if (!await userManager.IsInRoleAsync(user, role))
                {
                    var addResult = await userManager.AddToRoleAsync(user, role);
                    if (!addResult.Succeeded)
                    {
                        var msg = string.Join("; ", addResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"DbSeeder: could not add user '{user.UserName}' to role '{role}': {msg}");
                    }
                }
            }

            // Standaard users + rollen koppelen
            var adminEmail = "admin@local";
            var moderatorEmail = "moderator@local";
            var userEmail = "user@local";

            var admin = await EnsureUserAsync(adminEmail, "Administrator", "Admin!123");
            await EnsureUserInRoleAsync(admin, RoleAdmin);

            var moderator = await EnsureUserAsync(moderatorEmail, "Moderator", "Moderator!123");
            await EnsureUserInRoleAsync(moderator, RoleModerator);

            var user = await EnsureUserAsync(userEmail, "User", "User!123");
            await EnsureUserInRoleAsync(user, RoleUser);

            // Sets naar DbSets (kortere namen)
            var exercises = ctx.Set<Exercise>();
            var workouts = ctx.Set<Workout>();
            var workoutExercises = ctx.Set<WorkoutExercise>();
            var sessions = ctx.Set<Session>();
            var sessionSets = ctx.Set<SessionSet>();

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

            // Lege/naamloze sessies opschonen (incl. verweesde sets)
            var untitled = await sessions
                .Where(s => string.IsNullOrWhiteSpace(s.Title))
                .Select(s => s.Id)
                .ToListAsync();

            if (untitled.Count > 0)
            {
                var orphanSets = await sessionSets
                    .Where(ss => untitled.Contains(ss.SessionId))
                    .ToListAsync();

                if (orphanSets.Count > 0)
                    sessionSets.RemoveRange(orphanSets);

                var toRemove = await sessions
                    .Where(s => untitled.Contains(s.Id))
                    .ToListAsync();

                sessions.RemoveRange(toRemove);

                await ctx.SaveChangesAsync();
            }
        }
    }
}
