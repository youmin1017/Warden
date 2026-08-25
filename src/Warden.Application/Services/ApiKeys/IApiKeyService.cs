using Warden.Application.Dtos.ApiKeys;

namespace Warden.Application.Services.ApiKeys;

/// <summary>Every method is scoped to the calling user's own keys — this is a self-service
/// personal-access-token feature, not an admin view over other users' keys.</summary>
public interface IApiKeyService
{
    Task<IReadOnlyList<ApiKeyDto>> GetAllForUserAsync(Guid userId, CancellationToken ct = default);

    Task<ApiKeyCreatedDto> CreateAsync(Guid userId, CreateApiKeyRequest request, CancellationToken ct = default);

    Task RevokeAsync(Guid userId, Guid id, CancellationToken ct = default);
}
