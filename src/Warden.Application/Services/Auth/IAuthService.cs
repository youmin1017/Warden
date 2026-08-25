using Warden.Application.Dtos.Auth;

namespace Warden.Application.Services.Auth;

public interface IAuthService
{
    Task<TokenPairDto> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    /// <summary>
    /// Resolves an authenticated OIDC identity to a first-party token pair: links to an existing
    /// unlinked account by email, auto-provisions a new (role-less) account if none exists, or
    /// rejects if the email is already linked to a different provider.
    /// </summary>
    Task<TokenPairDto> CompleteOidcLoginAsync(string provider, string subject, string email, string displayNameHint, CancellationToken ct = default);

    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
