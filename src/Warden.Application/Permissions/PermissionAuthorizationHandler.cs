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

        if (await resolver.HasPermissionAsync(roleNames, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
