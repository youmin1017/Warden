using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warden.Application.Dtos.Permissions;
using Warden.Application.Permissions;
using Warden.Application.Services.Permissions;
using Warden.Domain.Permissions;

namespace Warden.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/permissions")]
public class PermissionsController(IPermissionCatalogService catalogService) : ControllerBase
{
    /// <summary>Every permission key declared in code via [RequirePermission], grouped by module — the rows of the admin permission matrix.</summary>
    [HttpGet("catalog")]
    [RequirePermission(PermissionConstants.Roles.Read)]
    public ActionResult<IReadOnlyList<PermissionModuleGroupDto>> GetCatalog()
        => Ok(catalogService.GetCatalogGroupedByModule());
}
