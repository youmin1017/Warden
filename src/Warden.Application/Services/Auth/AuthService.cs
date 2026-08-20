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
        var existing = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (existing is null || !existing.IsActive)
        {
            throw new UnauthorizedAppException("Refresh token is invalid or expired.");
        }

        existing.RevokedAtUtc = DateTime.UtcNow;

        var pair = await IssueTokenPairAsync(existing.User.Id, existing.User.Email, existing.User.UserRoles.Select(ur => ur.Role.Name).ToList(), ct, saveChanges: false);

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
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundAppException("User not found.");

        var roleNames = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var permissions = await permissionResolver.GetEffectivePermissionsAsync(roleNames, ct);

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, roleNames, permissions.OrderBy(p => p, StringComparer.Ordinal).ToList());
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
