using Microsoft.EntityFrameworkCore;
using Warden.Application.Common;
using Warden.Application.Dtos.Roles;
using Warden.Application.Permissions;
using Warden.Domain.Entities;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Services.Roles;

public class RoleService(AppDbContext db, IPermissionResolver permissionResolver) : IRoleService
{
    public async Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await db.Roles
            .Include(r => r.UserRoles)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

        return roles.Select(r => new RoleDto(r.Id, r.Name, r.Description, r.IsSystemRole, r.UserRoles.Count)).ToList();
    }

    public async Task<RoleDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var role = await FindAsync(id, ct);
        return ToDetailDto(role);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        var normalizedName = request.Name.Trim().ToUpperInvariant();
        if (await db.Roles.AnyAsync(r => r.NormalizedName == normalizedName, ct))
        {
            throw new ConflictAppException($"A role named '{request.Name}' already exists.");
        }

        var role = new AppRole
        {
            Name = request.Name.Trim(),
            NormalizedName = normalizedName,
            Description = request.Description,
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        return ToDetailDto(role);
    }

    public async Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        var role = await FindAsync(id, ct);
        var oldNormalizedName = role.NormalizedName;

        role.Name = request.Name.Trim();
        role.NormalizedName = request.Name.Trim().ToUpperInvariant();
        role.Description = request.Description;

        await db.SaveChangesAsync(ct);
        permissionResolver.InvalidateRole(oldNormalizedName);
        permissionResolver.InvalidateRole(role.NormalizedName);

        return ToDetailDto(role);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await FindAsync(id, ct);
        if (role.IsSystemRole)
        {
            throw new ConflictAppException("System roles cannot be deleted.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        permissionResolver.InvalidateRole(role.NormalizedName);
    }

    public async Task<RoleDetailDto> UpdatePermissionsAsync(Guid id, UpdateRolePermissionsRequest request, CancellationToken ct = default)
    {
        var role = await FindAsync(id, ct);

        // Removed via DbSet, not role.RolePermissions.Clear()/AddRange() — mutating the tracked
        // navigation collection in place makes EF misread the new rows (client-generated Guid
        // keys) as updates to the just-removed ones instead of inserts.
        db.RolePermissions.RemoveRange(role.RolePermissions);
        var newPermissions = request.Permissions
            .Distinct(StringComparer.Ordinal)
            .Select(key => new RolePermission { RoleId = role.Id, PermissionKey = key })
            .ToList();
        db.RolePermissions.AddRange(newPermissions);

        await db.SaveChangesAsync(ct);
        permissionResolver.InvalidateRole(role.NormalizedName);

        return new RoleDetailDto(
            role.Id,
            role.Name,
            role.Description,
            role.IsSystemRole,
            newPermissions.Select(p => p.PermissionKey).OrderBy(k => k, StringComparer.Ordinal).ToList());
    }

    private async Task<AppRole> FindAsync(Guid id, CancellationToken ct)
    {
        return await db.Roles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == id, ct)
            ?? throw new NotFoundAppException($"Role '{id}' was not found.");
    }

    private static RoleDetailDto ToDetailDto(AppRole role) => new(
        role.Id,
        role.Name,
        role.Description,
        role.IsSystemRole,
        role.RolePermissions.Select(rp => rp.PermissionKey).OrderBy(k => k, StringComparer.Ordinal).ToList());
}
