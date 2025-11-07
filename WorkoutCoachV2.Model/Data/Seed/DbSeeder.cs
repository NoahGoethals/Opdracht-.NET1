using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.Model.Data.Seed
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider sp)
        {
            // 1) Haal de connection string ZEKER op (met fallback) en migreer met een losse context.
            //    Dit omzeilt het hele "Name=DefaultConnection" gedoe.
            var cfg = sp.GetRequiredService<IConfiguration>();
            var cs = cfg.GetConnectionString("DefaultConnection")
                     ?? "Server=(localdb)\\MSSQLLocalDB;Database=WorkoutCoachV2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(cs)
                .Options;

            // Migreer met one-off context, zodat de DB er stáát voor we managers gebruiken.
            using (var ensure = new AppDbContext(options))
            {
                await ensure.Database.MigrateAsync();
            }

            // 2) Vanaf hier gewone DI-objects gebruiken
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Rollen
            foreach (var r in new[] { "Admin", "User" })
                if (!await roleMgr.RoleExistsAsync(r))
                    await roleMgr.CreateAsync(new IdentityRole(r));

            // Admin user
            if (await userMgr.FindByNameAsync("admin") is null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@local",
                    DisplayName = "Administrator",
                    EmailConfirmed = true
                };

                var res = await userMgr.CreateAsync(admin, "Admin!123");
                if (res.Succeeded)
                    await userMgr.AddToRoleAsync(admin, "Admin");
                else
                    throw new InvalidOperationException(
                        "Kon admin niet aanmaken: " + string.Join("; ", res.Errors.Select(e => e.Description)));
            }

            // Demo data
            if (!db.Exercises.Any())
            {
                db.Exercises.AddRange(
                    new Exercise { Name = "Bench Press" },
                    new Exercise { Name = "Back Squat" }
                );
            }

            await db.SaveChangesAsync();
        }
    }
}
