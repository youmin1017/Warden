using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Warden.Application.Dtos.Auth;
using Warden.Application.Options;
using Warden.Application.Services.Auth;
using Warden.WebApi.Common;

namespace Warden.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, IOidcHandoffCodeStore handoffCodeStore, IOptions<OidcOptions> oidcOptions) : ControllerBase
{
    [HttpGet("oidc/providers")]
    [AllowAnonymous]
    public ActionResult<IReadOnlyList<OidcProviderDto>> GetOidcProviders()
        => Ok(oidcOptions.Value.Providers.Select(p => new OidcProviderDto(p.Name, p.DisplayName)).ToList());

    [HttpGet("oidc/{provider}/challenge")]
    [AllowAnonymous]
    public IActionResult Challenge(string provider)
    {
        if (!oidcOptions.Value.Providers.Any(p => p.Name == provider))
        {
            return NotFound();
        }

        return base.Challenge(new AuthenticationProperties(), provider);
    }

    [HttpPost("oidc/exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairDto>> ExchangeOidcCode(OidcExchangeRequest request, CancellationToken ct)
    {
        var pair = await handoffCodeStore.ConsumeAsync(request.Code, ct);
        return pair is null ? Unauthorized() : Ok(pair);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<TokenPairDto>> Refresh(RefreshRequest request, CancellationToken ct)
        => Ok(await authService.RefreshAsync(request, ct));

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken ct)
        => Ok(await authService.GetCurrentUserAsync(User.GetUserId(), ct));
}
