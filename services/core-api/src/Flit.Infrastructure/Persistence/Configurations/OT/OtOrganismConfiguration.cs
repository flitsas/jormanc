using Flit.Infrastructure.Persistence.Entities.OT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.OT;

public sealed class OtOrganismConfiguration : IEntityTypeConfiguration<OtOrganism>
{
    public void Configure(EntityTypeBuilder<OtOrganism> builder)
    {
        builder.ToTable("ot_organisms", "ot");
        builder.HasKey(o => o.Id).HasName("pk_ot_organisms");
        builder.Property(o => o.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(o => o.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(o => o.Slug).HasColumnName("slug").IsRequired();
        builder.Property(o => o.Name).HasColumnName("name").IsRequired();
        builder.Property(o => o.Mode).HasColumnName("mode").HasDefaultValue("dashboard").IsRequired();
        builder.Property(o => o.QuipuxEnabled).HasColumnName("quipux_enabled").HasDefaultValue(false);
        builder.Property(o => o.QuipuxConfig).HasColumnName("quipux_config").HasColumnType("jsonb");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(o => o.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");
        builder.Property(o => o.DeletedBy).HasColumnName("deleted_by");
        builder.Property(o => o.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasIndex(o => new { o.TenantId, o.Slug }).IsUnique().HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_ot_organisms_tenant_slug");

        builder.HasQueryFilter(o => o.DeletedAt == null);
    }
}
