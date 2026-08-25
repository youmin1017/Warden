using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace Warden.Application.Permissions;

public class PermissionAuthorizationHandler(IPermissionResolver resolver) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roleNames = context.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
        if (roleNames.Length == 0)
        {
            return;
        }

        if (!await resolver.HasPermissionAsync(roleNames, requirement.Permission))
        {
            return;
        }

        // A scoped API key can only ever narrow its owner's role permissions, never widen them —
        // the role check above already gated on the owner's own grants.
        var isApiKeyAuth = context.User.HasClaim(ApiKeyAuthClaims.AuthMethod, ApiKeyAuthClaims.ApiKeyAuthMethodValue);
        if (isApiKeyAuth)
        {
            var scopes = context.User.FindAll(ApiKeyAuthClaims.Scope).Select(c => c.Value).ToArray();
            if (!PermissionMatcher.Matches(scopes, requirement.Permission))
            {
                return;
            }
        }

        context.Succeed(requirement);
    }
}
