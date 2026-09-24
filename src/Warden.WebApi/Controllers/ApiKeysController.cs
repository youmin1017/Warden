using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warden.Application.Dtos.ApiKeys;
using Warden.Application.Permissions;
using Warden.Application.Services.ApiKeys;
using Warden.Domain.Permissions;
using Warden.WebApi.Common;

namespace Warden.WebApi.Controllers;

/// <summary>Every action is scoped to the caller's own keys — this is a self-service
/// personal-access-token feature, not an admin view over other users' keys.</summary>
[ApiController]
[Authorize]
[Route("api/admin/api-keys")]
public class ApiKeysController(IApiKeyService apiKeyService) : ControllerBase
{
    [HttpGet]
    [RequirePermission(PermissionConstants.ApiKeys.Read)]
    public async Task<ActionResult<IReadOnlyList<ApiKeyDto>>> GetAll(CancellationToken ct)
        => Ok(await apiKeyService.GetAllForUserAsync(User.GetUserId(), ct));

    [HttpPost]
    [RequirePermission(PermissionConstants.ApiKeys.Create)]
    public async Task<ActionResult<ApiKeyCreatedDto>> Create(CreateApiKeyRequest request, CancellationToken ct)
        => Ok(await apiKeyService.CreateAsync(User.GetUserId(), request, ct));

    [HttpPost("{id:guid}/revoke")]
    [RequirePermission(PermissionConstants.ApiKeys.Revoke)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await apiKeyService.RevokeAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(PermissionConstants.ApiKeys.Delete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await apiKeyService.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }
}
