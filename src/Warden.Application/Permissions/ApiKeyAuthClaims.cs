namespace Warden.Application.Permissions;

/// <summary>
/// Claim types the API key authentication handler stamps onto the principal it builds, so
/// <see cref="PermissionAuthorizationHandler"/> can tell a scoped API key request apart from a
/// normal JWT login and further restrict it to the key's granted scopes.
/// </summary>
public static class ApiKeyAuthClaims
{
    public const string AuthMethod = "auth_method";
    public const string ApiKeyAuthMethodValue = "apikey";

    /// <summary>One claim per permission key/wildcard the key was scoped to at creation.</summary>
    public const string Scope = "permission_scope";
}
