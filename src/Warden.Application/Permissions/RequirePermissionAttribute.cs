using Microsoft.AspNetCore.Authorization;

namespace Warden.Application.Permissions;

/// <summary>
/// Declares that an action requires a specific permission key, e.g. [RequirePermission(PermissionConstants.Users.Create)].
/// The permission catalog service reflects over these attributes to build the admin UI's permission matrix.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "perm:";

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PolicyPrefix + permission;
    }

    public string Permission { get; }
}
