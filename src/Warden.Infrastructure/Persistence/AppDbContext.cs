using Microsoft.EntityFrameworkCore;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence;

/// <summary>
/// Shared EF Core model. Concrete per-provider subclasses (<see cref="SqliteAppDbContext"/>,
/// <see cref="PostgresAppDbContext"/>) only add the provider's UseXxx() call and own their own
/// Migrations/ folder, since migrations are provider-specific SQL.
/// </summary>
public abstract class AppDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OidcHandoffCode> OidcHandoffCodes => Set<OidcHandoffCode>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<ApiKeyScope> ApiKeyScopes => Set<ApiKeyScope>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
