using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warden.Application.Dtos.Users;
using Warden.Application.Permissions;
using Warden.Application.Services.Users;
using Warden.Domain.Permissions;

namespace Warden.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/admin/users")]
public class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstants.Users.Read)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(CancellationToken ct)
        => Ok(await userService.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [RequirePermission(PermissionConstants.Users.Read)]
    public async Task<ActionResult<UserDto>> GetById(Guid id, CancellationToken ct)
        => Ok(await userService.GetByIdAsync(id, ct));

    [HttpPost]
    [RequirePermission(PermissionConstants.Users.Create)]
    public async Task<ActionResult<UserDto>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var created = await userService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(PermissionConstants.Users.Update)]
    public async Task<ActionResult<UserDto>> Update(Guid id, UpdateUserRequest request, CancellationToken ct)
        => Ok(await userService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.Users.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await userService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/change-password")]
    [RequirePermission(PermissionConstants.Users.Update)]
    public async Task<IActionResult> ChangePassword(Guid id, ChangePasswordRequest request, CancellationToken ct)
    {
        await userService.ChangePasswordAsync(id, request, ct);
        return NoContent();
    }
}
