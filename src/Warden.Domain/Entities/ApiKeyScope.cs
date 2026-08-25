namespace Warden.Domain.Entities;

public class ApiKeyScope
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ApiKeyId { get; set; }
    public ApiKey ApiKey { get; set; } = null!;

    /// <summary>Permission key such as "user.read" or a wildcard like "user.*". Further restricts
    /// the key owner's own effective role permissions — it can never grant more than the user has.</summary>
    public required string PermissionKey { get; set; }
}
