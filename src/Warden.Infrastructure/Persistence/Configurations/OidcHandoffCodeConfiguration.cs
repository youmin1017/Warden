using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence.Configurations;

public class OidcHandoffCodeConfiguration : IEntityTypeConfiguration<OidcHandoffCode>
{
    public void Configure(EntityTypeBuilder<OidcHandoffCode> builder)
    {
        builder.ToTable("OidcHandoffCodes");
        builder.HasKey(c => c.Code);

        builder.Property(c => c.Code).HasMaxLength(64);
        builder.Property(c => c.TokenPairJson).IsRequired();
    }
}
