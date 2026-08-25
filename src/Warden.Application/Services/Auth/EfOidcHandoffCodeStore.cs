using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Warden.Application.Dtos.Auth;
using Warden.Domain.Entities;
using Warden.Infrastructure.Persistence;

namespace Warden.Application.Services.Auth;

public class EfOidcHandoffCodeStore(AppDbContext db) : IOidcHandoffCodeStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<string> CreateAsync(TokenPairDto pair, CancellationToken ct = default)
    {
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        db.OidcHandoffCodes.Add(new OidcHandoffCode
        {
            Code = code,
            TokenPairJson = JsonSerializer.Serialize(pair),
            ExpiresAtUtc = DateTime.UtcNow.Add(Ttl),
        });
        await db.SaveChangesAsync(ct);

        return code;
    }

    public async Task<TokenPairDto?> ConsumeAsync(string code, CancellationToken ct = default)
    {
        var entry = await db.OidcHandoffCodes.FirstOrDefaultAsync(c => c.Code == code, ct);
        var isValid = entry is not null && entry.ExpiresAtUtc >= DateTime.UtcNow;

        if (entry is not null)
        {
            db.OidcHandoffCodes.Remove(entry);
        }

        // Opportunistic sweep of other stale rows — this table only ever holds a handful of
        // short-lived rows, so a dedicated cleanup job isn't warranted.
        var expired = await db.OidcHandoffCodes
            .Where(c => c.Code != code && c.ExpiresAtUtc < DateTime.UtcNow)
            .ToListAsync(ct);
        db.OidcHandoffCodes.RemoveRange(expired);

        await db.SaveChangesAsync(ct);

        return isValid ? JsonSerializer.Deserialize<TokenPairDto>(entry!.TokenPairJson) : null;
    }
}
