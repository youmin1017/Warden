using Facet;
using Warden.Domain.Entities;

namespace Warden.Application.Dtos.Roles;

[Facet(typeof(AppRole), exclude: [nameof(AppRole.NormalizedName), nameof(AppRole.CreatedAtUtc), nameof(AppRole.UserRoles), nameof(AppRole.RolePermissions)])]
public partial record RoleDto
{
    public int UserCount { get; set; }
}

[Facet(typeof(AppRole), exclude: [nameof(AppRole.NormalizedName), nameof(AppRole.CreatedAtUtc), nameof(AppRole.UserRoles), nameof(AppRole.RolePermissions)])]
public partial record RoleDetailDto
{
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

[Facet(typeof(AppRole), Include = [nameof(AppRole.Name), nameof(AppRole.Description)])]
public partial record CreateRoleRequest;

[Facet(typeof(AppRole), Include = [nameof(AppRole.Name), nameof(AppRole.Description)])]
public partial record UpdateRoleRequest;

public record UpdateRolePermissionsRequest(IReadOnlyList<string> Permissions);
