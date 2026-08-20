using Microsoft.EntityFrameworkCore;
using Warden.Application.Common;
using Warden.Application.Dtos.Users;
using Warden.Domain.Entities;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Services.Users;

public class UserService(AppDbContext db) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Email)
            .ToListAsync(ct);

        return users.Select(u => ToDto(u)).ToList();
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await FindAsync(id, ct);
        return ToDto(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct))
        {
            throw new ConflictAppException($"A user with email '{request.Email}' already exists.");
        }

        var roles = await ResolveRolesAsync(request.RoleNames, ct);

        var user = new AppUser
        {
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            DisplayName = request.DisplayName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        user.UserRoles = roles.Select(r => new UserRole { UserId = user.Id, RoleId = r.Id }).ToList();

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return ToDto(user, roles.Select(r => r.Name).ToList());
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await FindAsync(id, ct);
        var roles = await ResolveRolesAsync(request.RoleNames, ct);

        user.DisplayName = request.DisplayName;
        user.IsActive = request.IsActive;
        // Removed via DbSet, not user.UserRoles.Clear()/AddRange() — see RoleService.UpdatePermissionsAsync
        // for why mutating the tracked navigation collection in place misleads EF's change tracker.
        db.UserRoles.RemoveRange(user.UserRoles);
        db.UserRoles.AddRange(roles.Select(r => new UserRole { UserId = user.Id, RoleId = r.Id }));

        await db.SaveChangesAsync(ct);
        return ToDto(user, roles.Select(r => r.Name).ToList());
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await FindAsync(id, ct);
        db.Users.Remove(user);
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid id, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await FindAsync(id, ct);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await db.SaveChangesAsync(ct);
    }

    private async Task<AppUser> FindAsync(Guid id, CancellationToken ct)
    {
        return await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new NotFoundAppException($"User '{id}' was not found.");
    }

    private async Task<List<AppRole>> ResolveRolesAsync(IReadOnlyList<string> roleNames, CancellationToken ct)
    {
        if (roleNames.Count == 0)
        {
            return [];
        }

        var normalized = roleNames.Select(r => r.ToUpperInvariant()).ToList();
        var roles = await db.Roles.Where(r => normalized.Contains(r.NormalizedName)).ToListAsync(ct);

        if (roles.Count != normalized.Distinct().Count())
        {
            var found = roles.Select(r => r.NormalizedName).ToHashSet();
            var missing = normalized.Distinct().Where(n => !found.Contains(n));
            throw new ValidationAppException($"Unknown role(s): {string.Join(", ", missing)}");
        }

        return roles;
    }

    private static UserDto ToDto(AppUser user, IReadOnlyList<string>? roleNamesOverride = null)
    {
        var roleNames = roleNamesOverride ?? user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return new UserDto(user.Id, user.Email, user.DisplayName, user.IsActive, roleNames, user.CreatedAtUtc);
    }
}
