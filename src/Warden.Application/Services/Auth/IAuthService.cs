using Warden.Application.Dtos.Auth;

namespace Warden.Application.Services.Auth;

public interface IAuthService
{
    Task<TokenPairDto> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<TokenPairDto> RefreshAsync(RefreshRequest request, CancellationToken ct = default);

    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task<CurrentUserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}
