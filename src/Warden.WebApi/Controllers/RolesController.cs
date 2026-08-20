using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warden.Application.Dtos.Roles;
using Warden.Application.Permissions;
using Warden.Application.Services.Roles;
using Warden.Domain.Permissions;

namespace Warden.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/roles")]
public class RolesController(IRoleService roleService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstants.Roles.Read)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAll(CancellationToken ct)
        => Ok(await roleService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionConstants.Roles.Read)]
    public async Task<ActionResult<RoleDetailDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await roleService.GetByIdAsync(id, ct));

    [HttpPost]
    [RequirePermission(PermissionConstants.Roles.Create)]
    public async Task<ActionResult<RoleDetailDto>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var created = await roleService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionConstants.Roles.Update)]
    public async Task<ActionResult<RoleDetailDto>> Update(Guid id, UpdateRoleRequest request, CancellationToken ct)
        => Ok(await roleService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.Roles.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await roleService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/permissions")]
    [RequirePermission(PermissionConstants.Roles.ManagePermissions)]
    public async Task<ActionResult<RoleDetailDto>> UpdatePermissions(Guid id, UpdateRolePermissionsRequest request, CancellationToken ct)
        => Ok(await roleService.UpdatePermissionsAsync(id, request, ct));
}
