using Microsoft.AspNetCore.Authorization;

namespace Warden.Application.Permissions;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
