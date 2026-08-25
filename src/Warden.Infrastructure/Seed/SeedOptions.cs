namespace Warden.Infrastructure.Seed;

public class SeedOptions
{
    public const string SectionName = "SeedAdmin";

    public string Email { get; set; } = "admin@warden.local";
    public string DisplayName { get; set; } = "Administrator";
}
