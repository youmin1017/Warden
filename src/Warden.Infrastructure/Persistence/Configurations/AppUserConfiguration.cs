using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(u => u.AuthProvider).HasMaxLength(64);
        builder.Property(u => u.ExternalSubject).HasMaxLength(256);

        builder.HasIndex(u => u.NormalizedEmail).IsUnique();
        builder.HasIndex(u => new { u.AuthProvider, u.ExternalSubject })
            .IsUnique()
            .HasFilter("\"AuthProvider\" IS NOT NULL AND \"ExternalSubject\" IS NOT NULL");
    }
}
