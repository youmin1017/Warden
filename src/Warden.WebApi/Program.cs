using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Warden.Application;
using Warden.Application.Options;
using Warden.Application.Services.ApiKeys;
using Warden.Application.Services.Auth;
using Warden.Infrastructure;
using Warden.Infrastructure.Persistence;
using Warden.Infrastructure.Seed;
using Warden.WebApi.Authentication;
using Warden.WebApi.Middleware;
using Scalar.AspNetCore;
using DotNetEnv;

// Dev convenience only, mirroring the frontend's auto-loaded .env — CI/production configure via
// real environment variables or user-secrets, so a missing .env here must not stop the app.
if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
{
    try
    {
        Env.TraversePath().Load();
    }
    catch (FileNotFoundException)
    {
    }
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.Configure<OidcOptions>(builder.Configuration.GetSection(OidcOptions.SectionName));

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddScoped<DatabaseSeeder>();

builder.Services.AddControllers();
builder.Services.AddAuthorization();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Missing Jwt configuration section.");

var oidcOptions = builder.Configuration.GetSection(OidcOptions.SectionName).Get<OidcOptions>()
    ?? throw new InvalidOperationException("Missing Oidc configuration section.");

// "Bearer" is a policy scheme, not a real handler: it forwards to "Jwt" or "ApiKey" by sniffing
// the token shape, so [Authorize] call sites (and the OpenAPI "Bearer" doc scheme) don't need to
// know or care which one actually authenticated the request.
var authBuilder = builder.Services
    .AddAuthentication("Bearer")
    .AddPolicyScheme("Bearer", "JWT or API Key", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var header = context.Request.Headers.Authorization.ToString();
            var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..] : header;
            return token.StartsWith(ApiKeyFormat.Prefix, StringComparison.Ordinal)
                ? ApiKeyAuthenticationDefaults.AuthenticationScheme
                : "Jwt";
        };
    })
    .AddJwtBearer("Jwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    })
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.AuthenticationScheme, _ => { })
    // Purely a transient correlation cookie for the OIDC handshake (state/nonce/PKCE) — not the
    // app's session. The frontend never sees it and it's unrelated to `warden_refresh_token`.
    .AddCookie("OidcCorrelation", options =>
    {
        options.Cookie.Name = ".Warden.OidcCorrelation";
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

foreach (var provider in oidcOptions.Providers)
{
    authBuilder.AddOpenIdConnect(provider.Name, provider.DisplayName, options =>
    {
        options.SignInScheme = "OidcCorrelation";
        options.Authority = provider.Authority;
        options.ClientId = provider.ClientId;
        options.ClientSecret = provider.ClientSecret;
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = false;
        options.CallbackPath = $"/api/auth/callback/{provider.Name}";

        options.Scope.Clear();
        foreach (var scope in provider.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            options.Scope.Add(scope);
        }

        // Warden mints its own JWT/refresh pair below and hands it to the SPA via a one-time
        // handoff code — it never relies on the OIDC handler's own sign-in cookie, so every path
        // here ends in HandleResponse() to stop that default post-auth behavior from also running.
        options.Events.OnTicketReceived = async context =>
        {
            var principal = context.Principal!;
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email");
            var displayName = principal.FindFirstValue("name") ?? email ?? string.Empty;

            var services = context.HttpContext.RequestServices;

            if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email))
            {
                services.GetRequiredService<ILogger<Program>>()
                    .LogWarning("OIDC provider {Provider} did not return a sub/email claim.", provider.Name);
                context.Response.Redirect($"{oidcOptions.FrontendBaseUrl}/login?error=oidc_failed");
                context.HandleResponse();
                return;
            }

            try
            {
                var authService = services.GetRequiredService<IAuthService>();
                var handoffStore = services.GetRequiredService<IOidcHandoffCodeStore>();

                var pair = await authService.CompleteOidcLoginAsync(provider.Name, subject, email, displayName, context.HttpContext.RequestAborted);
                var code = await handoffStore.CreateAsync(pair, context.HttpContext.RequestAborted);

                context.Response.Redirect($"{oidcOptions.FrontendBaseUrl}/auth/callback?code={code}");
            }
            catch (Exception ex)
            {
                services.GetRequiredService<ILogger<Program>>()
                    .LogWarning(ex, "OIDC login via {Provider} failed to complete.", provider.Name);
                context.Response.Redirect($"{oidcOptions.FrontendBaseUrl}/login?error=oidc_failed");
            }

            context.HandleResponse();
        };

        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect($"{oidcOptions.FrontendBaseUrl}/login?error=oidc_failed");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new OpenApiInfo { Title = "Warden API", Version = "v1" };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste a JWT access token from POST /api/auth/oidc/exchange or /api/auth/refresh (no 'Bearer ' prefix needed).",
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document, null)] = [],
        });

        return Task.CompletedTask;
    });

    // [AllowAnonymous] endpoints (login/refresh/logout) shouldn't be labeled "Auth Required" —
    // an empty security array here overrides the document-level default set above.
    options.AddOperationTransformer((operation, context, cancellationToken) =>
    {
        var allowsAnonymous = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<IAllowAnonymous>()
            .Any();

        if (allowsAnonymous)
        {
            operation.Security = [];
        }

        return Task.CompletedTask;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
