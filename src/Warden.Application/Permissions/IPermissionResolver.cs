namespace Warden.Application.Permissions;

/// <summary>
/// Resolves the effective permission set for a set of role names, backed by a short-lived
/// cache so admin edits to role permissions take effect without forcing users to re-login.
/// </summary>
public interface IPermissionResolver
{
    Task<bool> HasPermissionAsync(IEnumerable<string> roleNames, string permission, CancellationToken ct = default);

    Task<IReadOnlySet<string>> GetEffectivePermissionsAsync(IEnumerable<string> roleNames, CancellationToken ct = default);

    /// <summary>Call after any RolePermission change so cached lookups pick up the new grants.</summary>
    void InvalidateRole(string roleName);
}
