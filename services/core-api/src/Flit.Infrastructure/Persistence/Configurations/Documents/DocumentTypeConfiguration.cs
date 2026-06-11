using Flit.Infrastructure.Persistence.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Documents;

public sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("document_types", "documents");
        builder.HasKey(t => t.Id).HasName("pk_document_types");
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.Name).HasColumnName("name").IsRequired();
        builder.Property(t => t.LoadType).HasColumnName("load_type").HasDefaultValue("carga").IsRequired();
        builder.Property(t => t.AllowedFormats).HasColumnName("allowed_formats")
            .HasColumnType("jsonb").HasDefaultValue("""["pdf"]""");
        builder.Property(t => t.MaxSizeMb).HasColumnName("max_size_mb").HasDefaultValue(10);
        builder.Property(t => t.IsReusable).HasColumnName("is_reusable").HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");
        builder.Property(t => t.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasIndex(t => new { t.TenantId, t.Name }).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_document_types_tenant_name");

        builder.HasQueryFilter(t => t.DeletedAt == null);
    }
}
