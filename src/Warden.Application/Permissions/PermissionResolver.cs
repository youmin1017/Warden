using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Permissions;

public class PermissionResolver(AppDbContext db, IMemoryCache cache) : IPermissionResolver
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private const string CacheKeyPrefix = "role-permissions:";

    public async Task<bool> HasPermissionAsync(IEnumerable<string> roleNames, string permission, CancellationToken ct = default)
    {
        var effective = await GetEffectivePermissionsAsync(roleNames, ct);
        return PermissionMatcher.Matches(effective, permission);
    }

    public async Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(IEnumerable<string> roleNames, CancellationToken ct = default)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in roleNames)
        {
            var rolePermissions = await GetRolePermissionsAsync(roleName, ct);
            result.UnionWith(rolePermissions);
        }

        return result;
    }

    // Cache key is always the normalized name so InvalidateRole hits regardless of whether the
    // caller passes a role's display name (JWT claims) or its normalized form (role edits).
    public void InvalidateRole(string roleName) => cache.Remove(CacheKeyPrefix + roleName.ToUpperInvariant());

    private Task<HashSet<string>> GetRolePermissionsAsync(string roleName, CancellationToken ct)
    {
        var normalizedName = roleName.ToUpperInvariant();

        return cache.GetOrCreateAsync(CacheKeyPrefix + normalizedName, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;

            var keys = await db.RolePermissions
                .Where(rp => rp.Role.NormalizedName == normalizedName)
                .Select(rp => rp.PermissionKey)
                .ToListAsync(ct);

            return new HashSet<string>(keys, StringComparer.Ordinal);
        })!;
    }
}
