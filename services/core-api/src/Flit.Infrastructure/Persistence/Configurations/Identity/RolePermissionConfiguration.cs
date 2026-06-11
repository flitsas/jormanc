using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions", "identity");
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId }).HasName("pk_role_permissions");
        builder.Property(rp => rp.RoleId).HasColumnName("role_id");
        builder.Property(rp => rp.PermissionId).HasColumnName("permission_id");

        builder.HasOne(rp => rp.Role).WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .HasConstraintName("fk_role_permissions_roles")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(rp => rp.Permission).WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .HasConstraintName("fk_role_permissions_permissions")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
