using Warden.Application.Dtos.Roles;

namespace Warden.Application.Services.Roles;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllAsync(CancellationToken ct = default);

    Task<RoleDetailDto> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<RoleDetailDto> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);

    Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    Task<RoleDetailDto> UpdatePermissionsAsync(Guid id, UpdateRolePermissionsRequest request, CancellationToken ct = default);
}
