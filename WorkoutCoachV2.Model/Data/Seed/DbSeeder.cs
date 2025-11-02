using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WorkoutCoachV2.Model.Identity;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await ctx.Database.MigrateAsync();

        var roleNames = new[] { "Admin", "Coach", "Member" };
        foreach (var r in roleNames)
            if (!await roles.RoleExistsAsync(r))
                await roles.CreateAsync(new IdentityRole(r));

        var adminEmail = "admin@local";
        var admin = await users.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                DisplayName = "Admin"
            };
            await users.CreateAsync(admin, "Admin!12345");
            await users.AddToRolesAsync(admin, roleNames);
        }

        if (!await ctx.Exercises.AnyAsync())
        {
            var squat = new Exercise { Name = "Back Squat", Category = "Legs" };
            var bench = new Exercise { Name = "Bench Press", Category = "Chest" };
            var row = new Exercise { Name = "Barbell Row", Category = "Back" };

            ctx.Exercises.AddRange(squat, bench, row);

            var w = new Workout { Title = "Starting Strength A", ScheduledOn = DateTime.Today };
            ctx.Workouts.Add(w);

            ctx.WorkoutExercises.AddRange(
                new WorkoutExercise { Workout = w, Exercise = squat, Sets = 5, Reps = 5 },
                new WorkoutExercise { Workout = w, Exercise = bench, Sets = 5, Reps = 5 },
                new WorkoutExercise { Workout = w, Exercise = row, Sets = 5, Reps = 5 }
            );

            await ctx.SaveChangesAsync();
        }
    }
}
