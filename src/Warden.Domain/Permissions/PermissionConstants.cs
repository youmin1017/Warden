namespace Warden.Domain.Permissions;

/// <summary>
/// Canonical permission keys. Add new module classes here as the app grows —
/// each key doubles as the string passed to [RequirePermission] on a controller action.
/// </summary>
public static class PermissionConstants
{
    /// <summary>Matches every permission key. Reserved for the seeded SuperAdmin role.</summary>
    public const string SuperAdmin = "*";

    public static class Users
    {
        public const string Module = "user";
        public const string All = "user.*";
        public const string Read = "user.read";
        public const string Create = "user.create";
        public const string Update = "user.update";
        public const string Delete = "user.delete";
    }

    public static class Roles
    {
        public const string Module = "role";
        public const string All = "role.*";
        public const string Read = "role.read";
        public const string Create = "role.create";
        public const string Update = "role.update";
        public const string Delete = "role.delete";
        public const string ManagePermissions = "role.manage-permissions";
    }
}
