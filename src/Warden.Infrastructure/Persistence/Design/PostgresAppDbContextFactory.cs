using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warden.Infrastructure.Persistence.Design;

/// <summary>Used only by `dotnet ef migrations add -o Migrations/Postgres --context PostgresAppDbContext`.</summary>
public class PostgresAppDbContextFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PostgresAppDbContext>();
        builder.UseNpgsql("Host=localhost;Port=5432;Database=warden;Username=postgres;Password=postgres");
        return new PostgresAppDbContext(builder.Options);
    }
}
