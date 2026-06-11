using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles", "identity");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId }).HasName("pk_user_roles");
        builder.Property(ur => ur.UserId).HasColumnName("user_id");
        builder.Property(ur => ur.RoleId).HasColumnName("role_id");
        builder.Property(ur => ur.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(ur => ur.AssignedAt).HasColumnName("assigned_at").HasDefaultValueSql("now()");
        builder.Property(ur => ur.AssignedBy).HasColumnName("assigned_by");

        builder.HasOne(ur => ur.User).WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .HasConstraintName("fk_user_roles_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ur => ur.Role).WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .HasConstraintName("fk_user_roles_roles")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ur => new { ur.UserId, ur.TenantId })
            .HasDatabaseName("ix_user_roles_user_id_tenant_id");
    }
}
