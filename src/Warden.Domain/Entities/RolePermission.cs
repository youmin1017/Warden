namespace Warden.Domain.Entities;

public class RolePermission
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoleId { get; set; }
    public AppRole Role { get; set; } = null!;

    /// <summary>Permission key such as "user.create" or a wildcard like "user.*" / "*".</summary>
    public required string PermissionKey { get; set; }
}
