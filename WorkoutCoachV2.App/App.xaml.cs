// WPF app-bootstrap: Host + DI opzetten, EF Core + Identity registreren, seeden, LoginWindow tonen.

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
        // Globale host: bevat DI-container, config en logging.
        public static IHost HostApp { get; private set; } = null!;

        // Startup: host bouwen, services registreren, database seeden, login tonen.
        protected override async void OnStartup(StartupEventArgs e)
        {
            HostApp = Host.CreateDefaultBuilder()
                // Config: basispad + optional appsettings.json + environment variables.
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.SetBasePath(AppContext.BaseDirectory)
                       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                })
                // Services: DbContext, Identity, app-services, viewmodels en windows.
                .ConfigureServices((ctx, services) =>
                {
                    // Connection string (appsettings.json of fallback naar LocalDB).
                    var cs = ctx.Configuration.GetConnectionString("DefaultConnection")
                             ?? "Server=(localdb)\\MSSQLLocalDB;Database=WorkoutCoachV2;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

                    // EF Core context (SQL Server).
                    services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

                    // Identity Core (ApplicationUser + rollen) met soepele wachtwoordregels.
                    services.AddIdentityCore<ApplicationUser>(opt =>
                    {
                        opt.Password.RequireNonAlphanumeric = false;
                        opt.Password.RequireUppercase = false;
                        opt.Password.RequiredLength = 6;
                    })
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<AppDbContext>();

                    // App-services (auth).
                    services.AddScoped<AuthService>();

                    // ViewModels voor tabs en shell.
                    services.AddScoped<MainViewModel>();
                    services.AddScoped<ExercisesViewModel>();
                    services.AddScoped<WorkoutsViewModel>();
                    services.AddScoped<SessionsViewModel>();

                    // Windows (via DI opvragen).
                    services.AddTransient<LoginWindow>();
                    services.AddTransient<MainWindow>();
                    services.AddTransient<RegisterWindow>();
                    services.AddTransient<UserAdminWindow>();
                })
                .Build();

            // Seed database + standaardrollen/gebruikers + demo-data.
            await DbSeeder.SeedAsync(HostApp.Services);

            // Start met login.
            var login = HostApp.Services.GetRequiredService<LoginWindow>();
            login.Show();

            base.OnStartup(e);
        }

        // Netjes opruimen bij afsluiten (host disposen).
        protected override async void OnExit(ExitEventArgs e)
        {
            if (HostApp is IAsyncDisposable d) await d.DisposeAsync();
            base.OnExit(e);
        }
    }
}
