namespace Warden.Application.Dtos.Users;

public record UserDto(Guid Id, string Email, string DisplayName, bool IsActive, IReadOnlyList<string> Roles, DateTime CreatedAtUtc);

public record CreateUserRequest(string Email, string DisplayName, string Password, IReadOnlyList<string> RoleNames);

public record UpdateUserRequest(string DisplayName, bool IsActive, IReadOnlyList<string> RoleNames);

public record ChangePasswordRequest(string NewPassword);
