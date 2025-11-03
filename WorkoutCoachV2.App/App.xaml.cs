using System;
using System.Windows;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkoutCoachV2.App.View;
using WorkoutCoachV2.App.ViewModels;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Data.Seed;
using WorkoutCoachV2.Model.Identity;

namespace WorkoutCoachV2.App
{
    public partial class App : Application
    {
        public static IHost HostApp { get; private set; } = default!;

        public App()
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            HostApp = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                    
                })
                .ConfigureServices((ctx, services) =>
                {
                    var cs = ctx.Configuration.GetConnectionString("Default")
                             ?? throw new InvalidOperationException(
                                 "Missing connection string 'Default' in appsettings.json of User Secrets.");

                    services.AddDbContext<AppDbContext>(opt =>
                        opt.UseSqlServer(cs, sql =>
                        {
                            sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                        }));

                    services
                        .AddIdentityCore<AppUser>(opt =>
                        {
                            opt.User.RequireUniqueEmail = true;
                        })
                        .AddRoles<IdentityRole>()
                        .AddEntityFrameworkStores<AppDbContext>();

                    services.AddTransient<DbSeeder>();

                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<ExercisesViewModel>();
                    services.AddTransient<WorkoutsViewModel>();
                    services.AddTransient<SessionsViewModel>();

                    services.AddSingleton<MainWindow>();
                    services.AddTransient<LoginWindow>();
                })
                .Build();

            this.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.Message, "Onverwachte fout", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    MessageBox.Show(ex.Message, "Onverwachte fout (background)", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await HostApp.StartAsync();

            try
            {
                using var scope = HostApp.Services.CreateScope();
                var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                await seeder.SeedAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Seeding mislukte: " + ex.Message,
                    "Initialisatie", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            var login = HostApp.Services.GetRequiredService<LoginWindow>();
            login.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                await HostApp.StopAsync();
            }
            finally
            {
                HostApp.Dispose();
            }
            base.OnExit(e);
        }
    }
}
