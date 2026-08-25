namespace Warden.Domain.Entities;

/// <summary>
/// A user-owned personal access token. The raw secret is shown to the user exactly once at
/// creation and is never persisted — only its SHA-256 hash is stored, alongside a short public
/// <see cref="KeyId"/> that lets authentication look up the row without scanning every hash.
/// </summary>
public class ApiKey
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;

    public required string Name { get; set; }

    /// <summary>Public lookup segment embedded in the raw key, e.g. "wdn_&lt;KeyId&gt;...".</summary>
    public required string KeyId { get; set; }

    /// <summary>SHA-256 hash of the secret segment; the raw secret is never persisted.</summary>
    public required string SecretHash { get; set; }

    /// <summary>Last 4 characters of the raw key, kept only so the UI can show "...ab12" after creation.</summary>
    public required string DisplaySuffix { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public DateTime? LastUsedAtUtc { get; set; }

    public List<ApiKeyScope> Scopes { get; set; } = [];

    public bool IsActive => RevokedAtUtc is null && (ExpiresAtUtc is null || ExpiresAtUtc > DateTime.UtcNow);
}
