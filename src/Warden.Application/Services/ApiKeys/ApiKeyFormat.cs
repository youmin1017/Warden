using System.Security.Cryptography;

namespace Warden.Application.Services.ApiKeys;

/// <summary>
/// Shape of a raw API key: "wdn_&lt;12-char hex KeyId&gt;_&lt;secret&gt;". <see cref="ApiKeyService"/> mints
/// keys with this shape; the authentication handler that verifies incoming requests parses them
/// back with <see cref="TryParse"/> — both sides must agree on it, hence the shared helper.
/// </summary>
public static class ApiKeyFormat
{
    public const string Prefix = "wdn_";
    private const int KeyIdLength = 12;

    public static (string RawKey, string KeyId, string Secret) Generate()
    {
        var keyId = Convert.ToHexString(RandomNumberGenerator.GetBytes(KeyIdLength / 2)).ToLowerInvariant();
        var secret = Base64Url(RandomNumberGenerator.GetBytes(32));
        return ($"{Prefix}{keyId}_{secret}", keyId, secret);
    }

    public static bool TryParse(string rawKey, out string keyId, out string secret)
    {
        keyId = "";
        secret = "";

        if (!rawKey.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = rawKey[Prefix.Length..];
        if (rest.Length <= KeyIdLength + 1 || rest[KeyIdLength] != '_')
        {
            return false;
        }

        keyId = rest[..KeyIdLength];
        secret = rest[(KeyIdLength + 1)..];
        return true;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
