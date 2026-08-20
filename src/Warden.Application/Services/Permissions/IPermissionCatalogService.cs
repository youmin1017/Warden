using Warden.Application.Dtos.Permissions;

namespace Warden.Application.Services.Permissions;

public interface IPermissionCatalogService
{
    /// <summary>All permission keys declared via [RequirePermission] across every controller action.</summary>
    IReadOnlyList<PermissionDescriptorDto> GetCatalog();

    IReadOnlyList<PermissionModuleGroupDto> GetCatalogGroupedByModule();
}
