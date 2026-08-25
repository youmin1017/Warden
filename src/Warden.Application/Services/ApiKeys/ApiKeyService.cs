using Microsoft.EntityFrameworkCore;
using Warden.Application.Common;
using Warden.Application.Dtos.ApiKeys;
using Warden.Application.Permissions;
using Warden.Application.Services.Auth;
using Warden.Domain.Entities;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Services.ApiKeys;

public class ApiKeyService(AppDbContext db, ITokenService tokenService, IPermissionResolver permissionResolver) : IApiKeyService
{
    public async Task<IReadOnlyList<ApiKeyDto>> GetAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var keys = await db.ApiKeys
            .Where(k => k.UserId == userId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .Select(k => new ApiKeyDto
            {
                Id = k.Id,
                Name = k.Name,
                KeyId = k.KeyId,
                DisplaySuffix = k.DisplaySuffix,
                CreatedAtUtc = k.CreatedAtUtc,
                ExpiresAtUtc = k.ExpiresAtUtc,
                RevokedAtUtc = k.RevokedAtUtc,
                LastUsedAtUtc = k.LastUsedAtUtc,
                // Just the unordered keys here — EF Core can't translate OrderBy with a custom
                // IComparer inside a correlated subquery, so the ordinal sort happens below instead.
                Scopes = k.Scopes.Select(s => s.PermissionKey).ToList(),
            })
            .ToListAsync(ct);

        foreach (var key in keys)
        {
            key.Scopes = key.Scopes.OrderBy(s => s, StringComparer.Ordinal).ToList();
        }

        return keys;
    }

    public async Task<ApiKeyCreatedDto> CreateAsync(Guid userId, CreateApiKeyRequest request, CancellationToken ct = default)
    {
        var name = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Name is required.");
        }

        var scopes = request.Scopes.Distinct(StringComparer.Ordinal).ToList();
        if (scopes.Count == 0)
        {
            throw new ValidationAppException("Select at least one permission scope.");
        }

        // A key can never grant more than its owner already has — the request's scopes are only
        // a *further* restriction, checked again on every call in PermissionAuthorizationHandler
        // once the owner's own role permissions can change after the key was created.
        var roleNames = await db.UserRoles.Where(ur => ur.UserId == userId).Select(ur => ur.Role.Name).ToListAsync(ct);
        var callerPermissions = await permissionResolver.GetEffectivePermissionsAsync(roleNames, ct);
        var ungranted = scopes.Where(s => !PermissionMatcher.Matches(callerPermissions, s)).ToList();
        if (ungranted.Count > 0)
        {
            throw new ValidationAppException($"You cannot grant a scope you don't have: {string.Join(", ", ungranted)}");
        }

        var (rawKey, keyId, secret) = ApiKeyFormat.Generate();

        var apiKey = new ApiKey
        {
            UserId = userId,
            Name = name,
            KeyId = keyId,
            SecretHash = tokenService.HashToken(secret),
            DisplaySuffix = rawKey[^4..],
            ExpiresAtUtc = request.ExpiresAtUtc,
        };
        db.ApiKeys.Add(apiKey);
        db.ApiKeyScopes.AddRange(scopes.Select(s => new ApiKeyScope { ApiKeyId = apiKey.Id, PermissionKey = s }));

        await db.SaveChangesAsync(ct);

        return new ApiKeyCreatedDto(new ApiKeyDto(apiKey) { Scopes = scopes }, rawKey);
    }

    public async Task RevokeAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var apiKey = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.UserId == userId, ct)
            ?? throw new NotFoundAppException($"API key '{id}' was not found.");

        if (apiKey.RevokedAtUtc is null)
        {
            apiKey.RevokedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
