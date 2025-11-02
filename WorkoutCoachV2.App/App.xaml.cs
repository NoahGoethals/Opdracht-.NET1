using System;
using System.Windows;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            HostApp = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((ctx, cfg) =>
                {
                    cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                       .AddEnvironmentVariables();
                })
                .ConfigureServices((ctx, services) =>
                {
                    var cs = ctx.Configuration.GetConnectionString("Default")
                             ?? throw new InvalidOperationException("Missing connstring 'Default'.");
                    services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(cs));

                    services.AddIdentityCore<AppUser>(o => { o.User.RequireUniqueEmail = true; })
                            .AddRoles<IdentityRole>()
                            .AddEntityFrameworkStores<AppDbContext>();

                    services.AddTransient<DbSeeder>();

                    
                    services.AddSingleton<MainViewModel>();
                    services.AddTransient<LoginViewModel>();

                     
                    services.AddSingleton<MainWindow>();
                    services.AddTransient<LoginWindow>();
                })
                .Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await HostApp.StartAsync();

            using (var scope = HostApp.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
                await seeder.SeedAsync();
            }

            var login = HostApp.Services.GetRequiredService<LoginWindow>();
            login.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await HostApp.StopAsync();
            HostApp.Dispose();
            base.OnExit(e);
        }
    }
}
