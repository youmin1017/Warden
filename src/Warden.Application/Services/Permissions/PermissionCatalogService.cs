using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Warden.Application.Dtos.Permissions;
using Warden.Application.Permissions;

namespace Warden.Application.Services.Permissions;

/// <summary>
/// Discovers the permission catalog by reflecting over every [RequirePermission] attribute on
/// controllers/actions — this is what makes the admin permission matrix reflect code without
/// a manual, hand-maintained list.
/// </summary>
public class PermissionCatalogService(IActionDescriptorCollectionProvider actionDescriptorProvider) : IPermissionCatalogService
{
    public IReadOnlyList<PermissionDescriptorDto> GetCatalog()
    {
        // Keyed by permission alone: the catalog is "what capabilities exist", not "which
        // actions reference them" — the same key legitimately guards multiple actions
        // (e.g. GetAll and GetById both require role.read) and must appear only once.
        var byKey = new Dictionary<string, PermissionDescriptorDto>(StringComparer.Ordinal);

        foreach (var descriptor in actionDescriptorProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
        {
            var attributes = descriptor.MethodInfo
                .GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true)
                .Concat(descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(RequirePermissionAttribute), inherit: true))
                .Cast<RequirePermissionAttribute>();

            foreach (var attribute in attributes)
            {
                if (byKey.ContainsKey(attribute.Permission))
                {
                    continue;
                }

                var httpMethod = descriptor.ActionConstraints?
                    .OfType<HttpMethodActionConstraint>()
                    .FirstOrDefault()?.HttpMethods.FirstOrDefault();

                var module = attribute.Permission.Contains('.')
                    ? attribute.Permission[..attribute.Permission.IndexOf('.')]
                    : attribute.Permission;

                byKey[attribute.Permission] = new PermissionDescriptorDto(attribute.Permission, module, descriptor.ControllerName, descriptor.ActionName, httpMethod);
            }
        }

        return byKey.Values
            .OrderBy(p => p.Module, StringComparer.Ordinal)
            .ThenBy(p => p.Key, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<PermissionModuleGroupDto> GetCatalogGroupedByModule()
    {
        return GetCatalog()
            .GroupBy(p => p.Module, StringComparer.Ordinal)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => new PermissionModuleGroupDto(g.Key, g.ToList()))
            .ToList();
    }
}
