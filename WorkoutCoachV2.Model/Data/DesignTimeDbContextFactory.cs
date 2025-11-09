// Design-time DbContext factory: nodig voor 'Add-Migration' / 'dotnet ef migrations' in tooling.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WorkoutCoachV2.Model.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Config laden (appsettings.json indien aanwezig + env vars)
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables();

        var config = builder.Build();

        // Connection string kiezen (fallback naar LocalDB naam 'WorkoutCoachV2Db')
        var conn = config.GetConnectionString("Default")
                   ?? @"Server=(localdb)\MSSQLLocalDB;Database=WorkoutCoachV2Db;Trusted_Connection=True;MultipleActiveResultSets=true";

        // DbContextOptions met SQL Server
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(conn)
            .Options;

        // Context teruggeven aan EF tooling
        return new AppDbContext(options);
    }
}
