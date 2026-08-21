using Facet;
using Warden.Domain.Entities;

namespace Warden.Application.Dtos.Users;

[Facet(typeof(AppUser), exclude: [nameof(AppUser.NormalizedEmail), nameof(AppUser.PasswordHash), nameof(AppUser.UserRoles), nameof(AppUser.RefreshTokens)])]
public partial record UserDto
{
    public IReadOnlyList<string> Roles { get; set; } = [];
}

public record CreateUserRequest(string Email, string DisplayName, string Password, IReadOnlyList<string> RoleNames);

public record UpdateUserRequest(string DisplayName, bool IsActive, IReadOnlyList<string> RoleNames);

public record ChangePasswordRequest(string NewPassword);
