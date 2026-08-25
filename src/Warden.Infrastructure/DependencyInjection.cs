using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Warden.Infrastructure.Persistence;

namespace Warden.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Wires the concrete DbContext selected by Database:Provider in configuration, resolved
    /// everywhere else through the shared abstract AppDbContext base type.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var providerName = configuration["Database:Provider"] ?? nameof(DatabaseProvider.Sqlite);
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Missing configuration: ConnectionStrings:Default");

        if (!Enum.TryParse<DatabaseProvider>(providerName, ignoreCase: true, out var provider))
        {
            throw new InvalidOperationException(
                $"Unsupported Database:Provider '{providerName}'. Supported values: {string.Join(", ", Enum.GetNames<DatabaseProvider>())}");
        }

        switch (provider)
        {
            case DatabaseProvider.Sqlite:
                services.AddDbContext<AppDbContext, SqliteAppDbContext>(options => options.UseSqlite(connectionString));
                break;
            case DatabaseProvider.Postgres:
                services.AddDbContext<AppDbContext, PostgresAppDbContext>(options => options.UseNpgsql(connectionString));
                break;
            case DatabaseProvider.MariaDb:
                services.AddDbContext<AppDbContext, MariaDbAppDbContext>(options =>
                    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
                break;
            default:
                throw new InvalidOperationException($"Unhandled provider '{provider}'.");
        }

        return services;
    }
}
