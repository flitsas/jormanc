using Flit.Infrastructure.Persistence.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Documents;

public sealed class DocumentTemplateConfiguration : IEntityTypeConfiguration<DocumentTemplate>
{
    public void Configure(EntityTypeBuilder<DocumentTemplate> builder)
    {
        builder.ToTable("document_templates", "documents");
        builder.HasKey(t => t.Id).HasName("pk_document_templates");
        builder.Property(t => t.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(t => t.DocumentTypeId).HasColumnName("document_type_id").IsRequired();
        builder.Property(t => t.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(t => t.Version).HasColumnName("version").IsRequired();
        builder.Property(t => t.ContentRef).HasColumnName("content_ref").IsRequired();
        builder.Property(t => t.Status).HasColumnName("status").HasDefaultValue("active").IsRequired();
        builder.Property(t => t.Notes).HasColumnName("notes");
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(t => t.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(t => t.DocumentType).WithMany(dt => dt.Templates)
            .HasForeignKey(t => t.DocumentTypeId)
            .HasConstraintName("fk_document_templates_document_types")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.DocumentTypeId, t.Version }).IsUnique()
            .HasDatabaseName("uq_template_doc_type_version");
        // Solo una versión activa por tipo de documento
        builder.HasIndex(t => new { t.DocumentTypeId, t.Status })
            .HasFilter("status = 'active'")
            .HasDatabaseName("ix_document_templates_active_per_type");
    }
}
