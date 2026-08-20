using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Warden.Domain.Entities;

namespace Warden.Infrastructure.Persistence.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Label).HasMaxLength(128).IsRequired();
        builder.Property(m => m.Path).HasMaxLength(256);
        builder.Property(m => m.RequiredPermission).HasMaxLength(128);

        // No cascading self-reference: MenuService re-parents children before deleting a node.
        builder.HasOne<MenuItem>().WithMany().HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
