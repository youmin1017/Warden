using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence.Configurations;

public class ApiKeyScopeConfiguration : IEntityTypeConfiguration<ApiKeyScope>
{
    public void Configure(EntityTypeBuilder<ApiKeyScope> builder)
    {
        builder.ToTable("ApiKeyScopes");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.PermissionKey).HasMaxLength(128).IsRequired();

        builder.HasOne(s => s.ApiKey).WithMany(k => k.Scopes).HasForeignKey(s => s.ApiKeyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.ApiKeyId, s.PermissionKey }).IsUnique();
    }
}
