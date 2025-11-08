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
            using var scope = services.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await ctx.Database.MigrateAsync();

          
            async Task EnsureRoleAsync(string roleName)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                    await roleManager.CreateAsync(new IdentityRole(roleName));
            }

            await EnsureRoleAsync("Admin");
            await EnsureRoleAsync("Member");

            
            var adminEmail = "admin@local";
            var memberEmail = "member@local";

            var admin = await userManager.FindByNameAsync(adminEmail);
            if (admin == null)
            {
                admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail
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
                    Email = memberEmail
                };
                await userManager.CreateAsync(member, "Member!123");
            }
            if (!await userManager.IsInRoleAsync(member, "Member"))
                await userManager.AddToRoleAsync(member, "Member");

           
            var exercises = ctx.Set<Exercise>();
            var workouts = ctx.Set<Workout>();
            var workoutExercises = ctx.Set<WorkoutExercise>();
            var sessions = ctx.Set<Session>();
            var sessionSets = ctx.Set<SessionSet>();

            if (!await exercises.AnyAsync())
            {
                exercises.AddRange(
                    new Exercise { Name = "Bench Press" },
                    new Exercise { Name = "Back Squat" },
                    new Exercise { Name = "Deadlift" }
                );
                await ctx.SaveChangesAsync();
            }

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

            var anyLinkForWorkout = await workoutExercises.AnyAsync(we =>
                EF.Property<int>(we, "WorkoutId") == workout.Id);

            if (!anyLinkForWorkout)
            {
                var firstExercise = await exercises.FirstAsync();
                workoutExercises.Add(new WorkoutExercise
                {
                    WorkoutId = workout.Id,
                    ExerciseId = firstExercise.Id
                });
                await ctx.SaveChangesAsync();
            }

            if (!await sessions.AnyAsync())
            {
                var s1 = new Session { Date = DateTime.Today.AddDays(-3) };
                var s2 = new Session { Date = DateTime.Today.AddDays(-10) };
                sessions.AddRange(s1, s2);
                await ctx.SaveChangesAsync();

                var ex = await exercises.FirstAsync();

                sessionSets.AddRange(
                    new SessionSet { SessionId = s1.Id, ExerciseId = ex.Id, SetNumber = 1, Reps = 8, Weight = 60, Note = "seed" },
                    new SessionSet { SessionId = s1.Id, ExerciseId = ex.Id, SetNumber = 2, Reps = 6, Weight = 70 },
                    new SessionSet { SessionId = s2.Id, ExerciseId = ex.Id, SetNumber = 1, Reps = 8, Weight = 57.5 }
                );

                await ctx.SaveChangesAsync();
            }

        }
    }
}
