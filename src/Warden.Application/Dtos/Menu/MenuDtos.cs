namespace Warden.Application.Dtos.Menu;

public record MenuItemDto(
    Guid Id,
    Guid? ParentId,
    string Label,
    string? Path,
    string? Icon,
    string? RequiredPermission,
    int SortOrder,
    bool IsActive,
    IReadOnlyList<MenuItemDto> Children);

public record UpsertMenuItemRequest(
    Guid? ParentId,
    string Label,
    string? Path,
    string? Icon,
    string? RequiredPermission,
    int SortOrder,
    bool IsActive);
