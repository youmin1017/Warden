namespace Warden.Domain.Entities;

/// <summary>
/// Single-use, short-lived bridge between an OIDC callback (handled entirely server-side) and the
/// SPA's normal login response shape: the backend stashes the minted <c>TokenPairDto</c> here keyed
/// by an opaque code, redirects the browser to the frontend with that code, and the frontend
/// immediately exchanges it via a POST for the actual token pair.
/// </summary>
public class OidcHandoffCode
{
    public required string Code { get; set; }
    public required string TokenPairJson { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
