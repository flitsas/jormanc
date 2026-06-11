using Flit.Infrastructure.Persistence.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Documents;

public sealed class ProcedureTypeDocumentConfiguration : IEntityTypeConfiguration<ProcedureTypeDocument>
{
    public void Configure(EntityTypeBuilder<ProcedureTypeDocument> builder)
    {
        builder.ToTable("procedure_type_documents", "documents");
        builder.HasKey(d => d.Id).HasName("pk_procedure_type_documents");
        builder.Property(d => d.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(d => d.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(d => d.DocumentTypeId).HasColumnName("document_type_id").IsRequired();
        builder.Property(d => d.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(d => d.IsRequired).HasColumnName("is_required").HasDefaultValue(true);
        builder.Property(d => d.OrderIndex).HasColumnName("order_index").HasDefaultValue(0);
        builder.Property(d => d.ActorDefinitionId).HasColumnName("actor_definition_id");
        builder.Property(d => d.AllowPartialConsolidation).HasColumnName("allow_partial_consolidation")
            .HasDefaultValue(false);
        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");
        builder.Property(d => d.DeletedBy).HasColumnName("deleted_by");
        builder.Property(d => d.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(d => d.DocumentType).WithMany(t => t.ProcedureTypeDocuments)
            .HasForeignKey(d => d.DocumentTypeId)
            .HasConstraintName("fk_procedure_type_documents_document_types")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.ProcedureTypeId, d.DocumentTypeId }).IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_proc_type_doc");
        builder.HasIndex(d => new { d.TenantId, d.ProcedureTypeId })
            .HasDatabaseName("ix_procedure_type_documents_tenant_type");

        builder.HasQueryFilter(d => d.DeletedAt == null);
    }
}
