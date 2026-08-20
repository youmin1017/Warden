using Warden.Application.Dtos.Menu;

namespace Warden.Application.Services.Menu;

public interface IMenuService
{
    /// <summary>Full, unfiltered tree for the admin menu editor.</summary>
    Task<IReadOnlyList<MenuItemDto>> GetTreeAsync(CancellationToken ct = default);

    /// <summary>Tree pruned to items the given roles are permitted to see.</summary>
    Task<IReadOnlyList<MenuItemDto>> GetTreeForRolesAsync(IReadOnlyList<string> roleNames, CancellationToken ct = default);

    Task<MenuItemDto> CreateAsync(UpsertMenuItemRequest request, CancellationToken ct = default);

    Task<MenuItemDto> UpdateAsync(Guid id, UpsertMenuItemRequest request, CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
