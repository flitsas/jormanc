using Flit.Infrastructure.Persistence.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Documents;

public sealed class ProcedureDocumentConfiguration : IEntityTypeConfiguration<ProcedureDocument>
{
    public void Configure(EntityTypeBuilder<ProcedureDocument> builder)
    {
        builder.ToTable("procedure_documents", "documents");
        builder.HasKey(d => d.Id).HasName("pk_procedure_documents");
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(d => d.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(d => d.DocumentTypeId).HasColumnName("document_type_id").IsRequired();
        builder.Property(d => d.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(d => d.TemplateVersionId).HasColumnName("template_version_id");
        builder.Property(d => d.Origin).HasColumnName("origin").HasDefaultValue("uploaded").IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").HasDefaultValue("pending").IsRequired();
        builder.Property(d => d.FileRef).HasColumnName("file_ref");
        builder.Property(d => d.FileName).HasColumnName("file_name");
        builder.Property(d => d.GenerationMetadata).HasColumnName("generation_metadata")
            .HasColumnType("jsonb");
        builder.Property(d => d.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(d => d.GeneratedAt).HasColumnName("generated_at");
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(d => d.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(d => d.DocumentType).WithMany()
            .HasForeignKey(d => d.DocumentTypeId)
            .HasConstraintName("fk_procedure_documents_document_types")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.TenantId, d.ProcedureId, d.Status })
            .HasDatabaseName("ix_procedure_documents_tenant_procedure_status");
    }
}
