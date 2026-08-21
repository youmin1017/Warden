using Facet;
using Warden.Domain.Entities;

namespace Warden.Application.Dtos.Auth;

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record TokenPairDto(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, DateTime RefreshTokenExpiresAtUtc);

[Facet(typeof(AppUser), Include = [nameof(AppUser.Id), nameof(AppUser.Email), nameof(AppUser.DisplayName)])]
public partial record CurrentUserDto
{
    public IReadOnlyList<string> Roles { get; set; } = [];
    public IReadOnlyList<string> Permissions { get; set; } = [];
}
