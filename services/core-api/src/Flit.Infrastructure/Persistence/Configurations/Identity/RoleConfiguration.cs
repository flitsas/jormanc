using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", "identity");
        builder.HasKey(r => r.Id).HasName("pk_roles");
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.Slug).HasColumnName("slug").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.Description).HasColumnName("description");
        builder.Property(r => r.IsSystem).HasColumnName("is_system").HasDefaultValue(false);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(r => r.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at");
        builder.Property(r => r.DeletedBy).HasColumnName("deleted_by");
        builder.Property(r => r.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(r => r.Tenant).WithMany(t => t.Roles)
            .HasForeignKey(r => r.TenantId)
            .HasConstraintName("fk_roles_tenants")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.Slug, r.TenantId }).IsUnique()
            .HasDatabaseName("uq_roles_slug_tenant");
        builder.HasIndex(r => r.TenantId).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_roles_tenant_id");

        builder.HasQueryFilter(r => r.DeletedAt == null);
    }
}
