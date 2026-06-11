using Flit.Infrastructure.Persistence.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Identity;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", "identity");
        builder.HasKey(t => t.Id).HasName("pk_tenants");
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasIndex(t => t.Slug).IsUnique().HasDatabaseName("uq_tenants_slug");
        builder.HasIndex(t => t.IsActive).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_tenants_is_active");

        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
