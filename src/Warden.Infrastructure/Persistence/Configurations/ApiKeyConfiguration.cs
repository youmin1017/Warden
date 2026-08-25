using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("ApiKeys");
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Name).HasMaxLength(128).IsRequired();
        builder.Property(k => k.KeyId).HasMaxLength(32).IsRequired();
        builder.Property(k => k.SecretHash).HasMaxLength(128).IsRequired();
        builder.Property(k => k.DisplaySuffix).HasMaxLength(8).IsRequired();
        builder.Ignore(k => k.IsActive);

        builder.HasOne(k => k.User).WithMany(u => u.ApiKeys).HasForeignKey(k => k.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(k => k.KeyId).IsUnique();
    }
}
