using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Warden.Domain.Entities;
using Warden.Domain.Permissions;
using Warden.Infrastructure.Persistence;

namespace Warden.Infrastructure.Seed;

/// <summary>
/// Idempotent startup seed: a SuperAdmin role (wildcard "*") and one seeded admin user.
/// </summary>
public class DatabaseSeeder(AppDbContext db, IOptions<SeedOptions> seedOptions)
{
    private readonly SeedOptions _options = seedOptions.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var superAdminRole = await SeedSuperAdminRoleAsync(ct);
        await SeedAdminUserAsync(superAdminRole, ct);
    }

    private async Task<AppRole> SeedSuperAdminRoleAsync(CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == "SUPERADMIN", ct);
        if (role is not null)
        {
            return role;
        }

        role = new AppRole
        {
            Name = "SuperAdmin",
            NormalizedName = "SUPERADMIN",
            Description = "Full access to every permission.",
            IsSystemRole = true,
        };
        role.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionKey = PermissionConstants.SuperAdmin });

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role;
    }

    private async Task SeedAdminUserAsync(AppRole superAdminRole, CancellationToken ct)
    {
        var normalizedEmail = _options.Email.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
        {
            return;
        }

        var user = new AppUser
        {
            Email = _options.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = _options.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_options.Password),
        };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superAdminRole.Id });

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
    }
}
