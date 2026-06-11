using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class FormFieldConfiguration : IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable("form_fields", "procedures_config");
        builder.HasKey(f => f.Id).HasName("pk_form_fields");
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(f => f.SectionId).HasColumnName("section_id").IsRequired();
        builder.Property(f => f.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(f => f.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(f => f.Slug).HasColumnName("slug").IsRequired();
        builder.Property(f => f.Name).HasColumnName("name").IsRequired();
        builder.Property(f => f.FieldType).HasColumnName("field_type").HasDefaultValue("text").IsRequired();
        builder.Property(f => f.IsRequired).HasColumnName("is_required").HasDefaultValue(false);
        builder.Property(f => f.Config).HasColumnName("config").HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(f => f.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(f => f.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at");
        builder.Property(f => f.DeletedBy).HasColumnName("deleted_by");
        builder.Property(f => f.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(f => f.Section).WithMany(s => s.FormFields)
            .HasForeignKey(f => f.SectionId)
            .HasConstraintName("fk_form_fields_form_sections")
            .OnDelete(DeleteBehavior.Cascade);

        // GIN index para búsquedas en config JSONB
        builder.HasIndex(f => f.Config).HasMethod("gin")
            .HasDatabaseName("ix_form_fields_config_gin");
        builder.HasIndex(f => new { f.TenantId, f.SectionId }).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_form_fields_tenant_section");

        builder.HasQueryFilter(f => f.DeletedAt == null);
    }
}
