using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Warden.Application.Permissions;
using Warden.Application.Services.ApiKeys;
using Warden.Application.Services.Auth;
using Warden.Infrastructure.Persistence;

namespace Warden.WebApi.Authentication;

public static class ApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}

/// <summary>
/// Authenticates requests bearing a "wdn_..." personal access token (see <see cref="ApiKeyFormat"/>)
/// instead of a JWT, resolving it to the same kind of claims principal <c>TokenService.CreateAccessToken</c>
/// would produce — same user id/email/role claims — plus scope claims if the key was created with
/// a restricted set of permissions, so downstream authorization code doesn't need to know which
/// scheme authenticated the request.
/// </summary>
public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AppDbContext db,
    ITokenService tokenService)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var rawKey = header["Bearer ".Length..].Trim();
        if (!ApiKeyFormat.TryParse(rawKey, out var keyId, out var secret))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = await db.ApiKeys
            .Include(k => k.Scopes)
            .Include(k => k.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(k => k.KeyId == keyId);

        if (apiKey is null || !SecretMatches(apiKey.SecretHash, tokenService.HashToken(secret)))
        {
            return AuthenticateResult.Fail("Invalid API key.");
        }

        if (!apiKey.IsActive)
        {
            return AuthenticateResult.Fail("API key is revoked or expired.");
        }

        if (!apiKey.User.IsActive)
        {
            return AuthenticateResult.Fail("Account is disabled.");
        }

        // Best-effort usage tracking, throttled so a hot API key doesn't turn into a write on
        // every single request.
        if (apiKey.LastUsedAtUtc is null || apiKey.LastUsedAtUtc < DateTime.UtcNow.AddMinutes(-1))
        {
            apiKey.LastUsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        var identity = new ClaimsIdentity(Scheme.Name);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, apiKey.UserId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, apiKey.User.Email));
        identity.AddClaim(new Claim(ApiKeyAuthClaims.AuthMethod, ApiKeyAuthClaims.ApiKeyAuthMethodValue));
        foreach (var role in apiKey.User.UserRoles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Role.Name));
        }
        foreach (var scope in apiKey.Scopes)
        {
            identity.AddClaim(new Claim(ApiKeyAuthClaims.Scope, scope.PermissionKey));
        }

        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    private static bool SecretMatches(string storedHash, string candidateHash) =>
        CryptographicOperations.FixedTimeEquals(Convert.FromHexString(candidateHash), Convert.FromHexString(storedHash));
}
