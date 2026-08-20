using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence.Configurations;

public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("Roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name).HasMaxLength(128).IsRequired();
        builder.Property(r => r.NormalizedName).HasMaxLength(128).IsRequired();

        builder.HasIndex(r => r.NormalizedName).IsUnique();
    }
}
