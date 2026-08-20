namespace Warden.Domain.Entities;

public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ParentId { get; set; }
    public required string Label { get; set; }
    public string? Path { get; set; }
    public string? Icon { get; set; }

    /// <summary>Permission key required to see this item; null means visible to any authenticated user.</summary>
    public string? RequiredPermission { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
