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
        public static async Task SeedAsync(IServiceProvider services)
        {
            // Scope uit DI halen (DbContext + Identity managers)
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
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }
            await EnsureRoleAsync("Admin");
            await EnsureRoleAsync("Member");

            // Standaard users (admin/member) + rollen koppelen
            var adminEmail = "admin@local";
            var memberEmail = "member@local";

            var admin = await userManager.FindByNameAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    DisplayName = "Administrator"
                };
                await userManager.CreateAsync(admin, "Admin!123");
            }
            if (!await userManager.IsInRoleAsync(admin, "Admin"))
                await userManager.AddToRoleAsync(admin, "Admin");

            var member = await userManager.FindByNameAsync(memberEmail);
            if (member == null)
            {
                member = new ApplicationUser
                {
                    UserName = memberEmail,
                    Email = memberEmail,
                    DisplayName = "Member"
                };
                await userManager.CreateAsync(member, "Member!123");
            }
            if (!await userManager.IsInRoleAsync(member, "Member"))
                await userManager.AddToRoleAsync(member, "Member");

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
                var orphanSets = await sessionSets.Where(ss => untitled.Contains(ss.SessionId)).ToListAsync();
                if (orphanSets.Count > 0)
                    sessionSets.RemoveRange(orphanSets);

                var toRemove = await sessions.Where(s => untitled.Contains(s.Id)).ToListAsync();
                sessions.RemoveRange(toRemove);

                await ctx.SaveChangesAsync();
            }

        }
    }
}
