using Microsoft.EntityFrameworkCore;
using Warden.Application.Common;
using Warden.Application.Dtos.Menu;
using Warden.Application.Permissions;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Services.Menu;

public class MenuService(AppDbContext db, IPermissionResolver permissionResolver) : IMenuService
{
    public async Task<IReadOnlyList<MenuItemDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var items = await db.MenuItems.OrderBy(m => m.SortOrder).ToListAsync(ct);
        return BuildTree(items, null);
    }

    public async Task<IReadOnlyList<MenuItemDto>> GetTreeForRolesAsync(IReadOnlyList<string> roleNames, CancellationToken ct = default)
    {
        var items = await db.MenuItems.Where(m => m.IsActive).OrderBy(m => m.SortOrder).ToListAsync(ct);
        var effectivePermissions = await permissionResolver.GetEffectivePermissionsAsync(roleNames, ct);

        var tree = BuildTree(items, null);
        return tree.Select(node => Prune(node, effectivePermissions)).OfType<MenuItemDto>().ToList();
    }

    public async Task<MenuItemDto> CreateAsync(UpsertMenuItemRequest request, CancellationToken ct = default)
    {
        var entity = new Domain.Entities.MenuItem
        {
            ParentId = request.ParentId,
            Label = request.Label,
            Path = request.Path,
            Icon = request.Icon,
            RequiredPermission = request.RequiredPermission,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive,
        };

        db.MenuItems.Add(entity);
        await db.SaveChangesAsync(ct);

        return ToLeafDto(entity);
    }

    public async Task<MenuItemDto> UpdateAsync(Guid id, UpsertMenuItemRequest request, CancellationToken ct = default)
    {
        var entity = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundAppException($"Menu item '{id}' was not found.");

        entity.ParentId = request.ParentId;
        entity.Label = request.Label;
        entity.Path = request.Path;
        entity.Icon = request.Icon;
        entity.RequiredPermission = request.RequiredPermission;
        entity.SortOrder = request.SortOrder;
        entity.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return ToLeafDto(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct)
            ?? throw new NotFoundAppException($"Menu item '{id}' was not found.");

        // Re-parent children to the deleted node's parent so the tree stays intact.
        var children = await db.MenuItems.Where(m => m.ParentId == id).ToListAsync(ct);
        foreach (var child in children)
        {
            child.ParentId = entity.ParentId;
        }

        db.MenuItems.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static List<MenuItemDto> BuildTree(List<Domain.Entities.MenuItem> items, Guid? parentId)
    {
        return items
            .Where(m => m.ParentId == parentId)
            .Select(m => new MenuItemDto(m.Id, m.ParentId, m.Label, m.Path, m.Icon, m.RequiredPermission, m.SortOrder, m.IsActive, BuildTree(items, m.Id)))
            .ToList();
    }

    private static MenuItemDto ToLeafDto(Domain.Entities.MenuItem m) =>
        new(m.Id, m.ParentId, m.Label, m.Path, m.Icon, m.RequiredPermission, m.SortOrder, m.IsActive, []);

    /// <summary>Keeps a node if the caller has its permission (or it has none), or if any child survives pruning.</summary>
    private static MenuItemDto? Prune(MenuItemDto node, IReadOnlySet<string> effectivePermissions)
    {
        var prunedChildren = node.Children
            .Select(child => Prune(child, effectivePermissions))
            .OfType<MenuItemDto>()
            .ToList();

        var visible = node.RequiredPermission is null || PermissionMatcher.Matches(effectivePermissions, node.RequiredPermission);

        if (!visible && prunedChildren.Count == 0)
        {
            return null;
        }

        return node with { Children = prunedChildren };
    }
}
