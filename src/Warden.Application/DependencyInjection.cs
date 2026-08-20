using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Warden.Application.Permissions;
using Warden.Application.Services.Auth;
using Warden.Application.Services.Menu;
using Warden.Application.Services.Permissions;
using Warden.Application.Services.Roles;
using Warden.Application.Services.Users;

namespace Warden.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMemoryCache();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IPermissionCatalogService, PermissionCatalogService>();
        services.AddScoped<IPermissionResolver, PermissionResolver>();

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
