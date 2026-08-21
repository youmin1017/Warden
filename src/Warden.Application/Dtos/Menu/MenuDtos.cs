using Facet;
using Warden.Domain.Entities;

namespace Warden.Application.Dtos.Menu;

[Facet(typeof(MenuItem))]
public partial record MenuItemDto
{
    public IReadOnlyList<MenuItemDto> Children { get; set; } = [];
}

[Facet(typeof(MenuItem), exclude: [nameof(MenuItem.Id)])]
public partial record UpsertMenuItemRequest;
