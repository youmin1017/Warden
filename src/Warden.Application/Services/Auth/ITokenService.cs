namespace Warden.Application.Services.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(Guid userId, string email, IReadOnlyList<string> roles);

    (string RawToken, string TokenHash, DateTime ExpiresAtUtc) CreateRefreshToken();

    string HashToken(string rawToken);
}
