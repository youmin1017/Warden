using Facet;
using Warden.Domain.Entities;

namespace Warden.Application.Dtos.ApiKeys;

[Facet(typeof(ApiKey), exclude: [nameof(ApiKey.UserId), nameof(ApiKey.User), nameof(ApiKey.SecretHash), nameof(ApiKey.Scopes), nameof(ApiKey.IsActive)])]
public partial record ApiKeyDto
{
    public IReadOnlyList<string> Scopes { get; set; } = [];
}

/// <summary>Returned only once, from the create endpoint — the raw secret is never retrievable again.</summary>
public record ApiKeyCreatedDto(ApiKeyDto ApiKey, string RawKey);

public record CreateApiKeyRequest(string Name, DateTime? ExpiresAtUtc, IReadOnlyList<string> Scopes);
