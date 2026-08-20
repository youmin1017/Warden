namespace Warden.Application.Dtos.Permissions;

public record PermissionDescriptorDto(string Key, string Module, string Controller, string Action, string? HttpMethod);

public record PermissionModuleGroupDto(string Module, IReadOnlyList<PermissionDescriptorDto> Permissions);
