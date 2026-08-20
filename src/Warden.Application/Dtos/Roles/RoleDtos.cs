namespace Warden.Application.Dtos.Roles;

public record RoleDto(Guid Id, string Name, string? Description, bool IsSystemRole, int UserCount);

public record RoleDetailDto(Guid Id, string Name, string? Description, bool IsSystemRole, IReadOnlyList<string> Permissions);

public record CreateRoleRequest(string Name, string? Description);

public record UpdateRoleRequest(string Name, string? Description);

public record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
