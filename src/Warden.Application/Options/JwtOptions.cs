namespace Warden.Application.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; set; }
    public required string Audience { get; set; }

    /// <summary>Symmetric signing secret. Must be at least 32 bytes once UTF8-encoded (HS256).</summary>
    public required string SigningKey { get; set; }

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 14;
}
