using Warden.Application.Dtos.Auth;

namespace Warden.Application.Services.Auth;

/// <summary>
/// Bridges the OIDC callback (handled entirely on the backend) back to the SPA: stores a freshly
/// minted token pair behind a short-lived, single-use opaque code the frontend exchanges via a
/// normal POST, so the pair never travels through a URL/browser history.
/// </summary>
public interface IOidcHandoffCodeStore
{
    Task<string> CreateAsync(TokenPairDto pair, CancellationToken ct = default);

    /// <summary>Looks up and immediately deletes the code — it is single-use regardless of outcome.</summary>
    Task<TokenPairDto?> ConsumeAsync(string code, CancellationToken ct = default);
}
