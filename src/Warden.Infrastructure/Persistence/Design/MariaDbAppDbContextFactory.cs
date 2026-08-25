using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warden.Infrastructure.Persistence.Design;

/// <summary>Used only by `dotnet ef migrations add -o Migrations/MariaDb --context MariaDbAppDbContext`.</summary>
public class MariaDbAppDbContextFactory : IDesignTimeDbContextFactory<MariaDbAppDbContext>
{
    public MariaDbAppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<MariaDbAppDbContext>();
        builder.UseMySql(
            "Server=localhost;Port=3306;Database=warden;User=root;Password=root;",
            new MariaDbServerVersion(new Version(10, 11)));
        return new MariaDbAppDbContext(builder.Options);
    }
}
