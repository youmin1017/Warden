namespace Warden.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Email { get; set; }
    public required string NormalizedEmail { get; set; }
    public required string DisplayName { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Name of the configured OIDC provider (Oidc:Providers[].Name) this account is linked to, if any.</summary>
    public string? AuthProvider { get; set; }
    /// <summary>The provider's `sub` claim. Null until the account is linked on first successful OIDC login.</summary>
    public string? ExternalSubject { get; set; }

    public List<UserRole> UserRoles { get; set; } = [];
    public List<RefreshToken> RefreshTokens { get; set; } = [];
    public List<ApiKey> ApiKeys { get; set; } = [];
}
