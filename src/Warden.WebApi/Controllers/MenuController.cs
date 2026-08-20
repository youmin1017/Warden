using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warden.Application.Dtos.Menu;
using Warden.Application.Permissions;
using Warden.Application.Services.Menu;
using Warden.Domain.Permissions;
using Warden.WebApi.Common;

namespace Warden.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/menu")]
public class MenuController(IMenuService menuService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstants.Menu.Read)]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetTree(CancellationToken ct)
        => Ok(await menuService.GetTreeAsync(ct));

    /// <summary>Menu pruned to what the caller may see — this is what the Nuxt sidebar renders.</summary>
    [HttpGet("for-current-user")]
    public async Task<ActionResult<IReadOnlyList<MenuItemDto>>> GetTreeForCurrentUser(CancellationToken ct)
        => Ok(await menuService.GetTreeForRolesAsync(User.GetRoleNames(), ct));

    [HttpPost]
    [RequirePermission(PermissionConstants.Menu.Create)]
    public async Task<ActionResult<MenuItemDto>> Create(UpsertMenuItemRequest request, CancellationToken ct)
        => Ok(await menuService.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionConstants.Menu.Update)]
    public async Task<ActionResult<MenuItemDto>> Update(Guid id, UpsertMenuItemRequest request, CancellationToken ct)
        => Ok(await menuService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.Menu.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await menuService.DeleteAsync(id, ct);
        return NoContent();
    }
}
