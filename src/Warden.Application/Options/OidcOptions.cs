namespace Warden.Application.Options;

public class OidcOptions
{
    public const string SectionName = "Oidc";

    /// <summary>Base URL of the Nuxt frontend, used to build the post-login redirect target.</summary>
    public required string FrontendBaseUrl { get; set; }

    public List<OidcProviderOptions> Providers { get; set; } = [];
}

public class OidcProviderOptions
{
    /// <summary>Stable, URL-safe identifier — used verbatim as the auth scheme name and in
    /// `/api/auth/oidc/{name}/challenge` and the `/api/auth/callback/{name}` callback path.</summary>
    public required string Name { get; set; }

    /// <summary>Shown on the "Sign in with ..." button.</summary>
    public required string DisplayName { get; set; }

    public required string Authority { get; set; }
    public required string ClientId { get; set; }
    public required string ClientSecret { get; set; }
    public string Scope { get; set; } = "openid profile email";
}
