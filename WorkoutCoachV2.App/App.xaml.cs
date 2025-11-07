using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using WorkoutCoachV2.App.Services;
using WorkoutCoachV2.App.View;
using WorkoutCoachV2.App.ViewModels;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Data.Seed;
using WorkoutCoachV2.Model.Models;

namespace WorkoutCoachV2.App
{
    public partial class App : Application
    {
        public static IHost HostApp { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            HostApp = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory)
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    var cs = ctx.Configuration.GetConnectionString("DefaultConnection")
                             ?? "Server=(localdb)\\MSSQLLocalDB;Database=WorkoutCoachV2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

                    services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

                    services.AddIdentityCore<ApplicationUser>(opt =>
                    {
                        opt.Password.RequireNonAlphanumeric = false;
                        opt.Password.RequireUppercase = false;
                        opt.Password.RequiredLength = 6;
                    })
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<AppDbContext>();

                    services.AddScoped<AuthService>();

                    
                    services.AddScoped<MainViewModel>();
                    services.AddScoped<ExercisesViewModel>();
                    services.AddScoped<WorkoutsViewModel>();
                    services.AddScoped<SessionsViewModel>();

                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                    services.AddTransient<RegisterWindow>();
                    services.AddTransient<UserAdminWindow>();
                })
                .Build();

            await DbSeeder.SeedAsync(HostApp.Services);

            var login = HostApp.Services.GetRequiredService<LoginWindow>();
            login.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            if (HostApp is IAsyncDisposable d) await d.DisposeAsync();
            base.OnExit(e);
        }
    }
}
