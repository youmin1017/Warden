namespace Warden.Application.Permissions;

/// <summary>
/// Pure wildcard matcher for permission keys, e.g. a granted "user.*" satisfies a
/// requested "user.create", and granted "*" satisfies anything.
/// </summary>
public static class PermissionMatcher
{
    public static bool Matches(IEnumerable<string> grantedKeys, string requestedKey)
    {
        foreach (var granted in grantedKeys)
        {
            if (Matches(granted, requestedKey))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Matches(string grantedKey, string requestedKey)
    {
        if (grantedKey == "*")
        {
            return true;
        }

        if (string.Equals(grantedKey, requestedKey, StringComparison.Ordinal))
        {
            return true;
        }

        if (grantedKey.EndsWith(".*", StringComparison.Ordinal))
        {
            var modulePrefix = grantedKey[..^1]; // "user.*" -> "user."
            return requestedKey.StartsWith(modulePrefix, StringComparison.Ordinal);
        }

        return false;
    }
}
