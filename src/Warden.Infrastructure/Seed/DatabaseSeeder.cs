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

    // Seeds an unlinked placeholder account (no AuthProvider/ExternalSubject) rather than a
    // usable local login — whoever controls SeedAdmin:Email on one of the configured OIDC
    // providers inherits SuperAdmin the first time they log in, via AuthService.CompleteOidcLoginAsync's
    // "found by email, not yet linked" path. Idempotently ensures both the row AND the role exist
    // (rather than just skipping if the email is already present), so a user who happened to be
    // auto-provisioned with no roles *before* this seeder first ran (e.g. re-deploy ordering) still
    // ends up with SuperAdmin on the next boot instead of being stuck role-less forever.
    private async Task SeedAdminUserAsync(AppRole superAdminRole, CancellationToken ct)
    {
        var normalizedEmail = _options.Email.Trim().ToUpperInvariant();
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null)
        {
            user = new AppUser
            {
                Email = _options.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                DisplayName = _options.DisplayName,
            };
            db.Users.Add(user);
        }

        if (!user.UserRoles.Any(ur => ur.RoleId == superAdminRole.Id))
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = superAdminRole.Id });
        }

        await db.SaveChangesAsync(ct);
    }
}
