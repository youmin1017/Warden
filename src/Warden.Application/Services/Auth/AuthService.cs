using Microsoft.EntityFrameworkCore;
using Warden.Application.Common;
using Warden.Application.Dtos.Auth;
using Warden.Application.Permissions;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Services.Auth;

public class AuthService(AppDbContext db, ITokenService tokenService, IPermissionResolver permissionResolver) : IAuthService
{
    public async Task<TokenPairDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        return await IssueTokenPairAsync(user.Id, user.Email, user.UserRoles.Select(ur => ur.Role.Name).ToList(), ct);
    }

    public async Task<TokenPairDto> RefreshAsync(RefreshRequest request, CancellationToken ct = default)
    {
        var hash = tokenService.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token is invalid or expired.");
        }

        // Projected instead of `.Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)`:
        // that Include tracked the whole user+role graph just to read two values, and fanned the
        // single-row RefreshToken lookup out into a multi-join result set.
        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == existing.UserId)
            .Select(u => new { u.Email, Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList() })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundAppException("User not found.");

        existing.RevokedAtUtc = DateTime.UtcNow;

        var pair = await IssueTokenPairAsync(existing.UserId, user.Email, user.Roles, ct, saveChanges: false);

        existing.ReplacedByTokenHash = tokenService.HashToken(pair.RefreshToken);
        await db.SaveChangesAsync(ct);

        return pair;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokenService.HashToken(refreshToken);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is not null && existing.RevokedAtUtc is null)
        {
            existing.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .Where(u => u.Id == userId)
            .Select(u => new CurrentUserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundAppException("User not found.");

        var permissions = await permissionResolver.GetEffectivePermissionsAsync(user.Roles, ct);

        return user with { Permissions = permissions.OrderBy(p => p, StringComparer.Ordinal).ToList() };
    }

    private async Task<TokenPairDto> IssueTokenPairAsync(Guid userId, string email, IReadOnlyList<string> roles, CancellationToken ct, bool saveChanges = true)
    {
        var (accessToken, accessExpiresAtUtc) = tokenService.CreateAccessToken(userId, email, roles);
        var (rawRefreshToken, refreshTokenHash, refreshExpiresAtUtc) = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new Domain.Entities.RefreshToken
        {
            UserId = userId,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = refreshExpiresAtUtc,
        });

        if (saveChanges)
        {
            await db.SaveChangesAsync(ct);
        }

        return new TokenPairDto(accessToken, accessExpiresAtUtc, rawRefreshToken, refreshExpiresAtUtc);
    }
}
