using CoroMES.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoroMES.Infrastructure;

/// <summary>
/// Design-time DbContext factory for EF Core migrations.
/// Configures PostgreSQL as the target database for migrations.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use PostgreSQL for migrations (production database)
        var connectionString = "Host=localhost;Port=5432;Database=coromes;Username=postgres;Password=changeme";
        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
