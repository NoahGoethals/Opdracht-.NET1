using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WorkoutCoachV2.Model.Data;
using WorkoutCoachV2.Model.Data.Seed;
using WorkoutCoachV2.Model.Identity;

namespace WorkoutCoachV2.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(cfg =>
            {
                cfg.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                cfg.AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                var conn = ctx.Configuration.GetConnectionString("Default")
                           ?? @"Server=(localdb)\MSSQLLocalDB;Database=WorkoutCoachV2Db;Trusted_Connection=True;MultipleActiveResultSets=true";

                services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

                services.AddIdentityCore<AppUser>(o =>
                {
                    o.Password.RequiredLength = 6;
                    o.Password.RequireNonAlphanumeric = false;
                    o.Password.RequireUppercase = false;
                })
                .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>();

                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();
        await DbSeeder.SeedAsync(_host.Services);

        var main = _host.Services.GetRequiredService<MainWindow>();
        main.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
            await _host.StopAsync();

        _host?.Dispose();
        base.OnExit(e);
    }
}
