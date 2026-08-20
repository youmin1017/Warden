using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Warden.Infrastructure.Persistence.Design;

/// <summary>Used only by `dotnet ef migrations add -o Migrations/Sqlite --context SqliteAppDbContext`.</summary>
public class SqliteAppDbContextFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SqliteAppDbContext>();
        builder.UseSqlite("Data Source=warden.design.db");
        return new SqliteAppDbContext(builder.Options);
    }
}
