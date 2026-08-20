using System.Security.Claims;

namespace Warden.WebApi.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("Authenticated principal has no NameIdentifier claim.");
        return Guid.Parse(value);
    }

    public static IReadOnlyList<string> GetRoleNames(this ClaimsPrincipal user) =>
        user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
}
