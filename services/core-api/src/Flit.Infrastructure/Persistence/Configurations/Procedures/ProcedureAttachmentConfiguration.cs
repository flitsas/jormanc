using Flit.Infrastructure.Persistence.Entities.Procedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Procedures;

public sealed class ProcedureAttachmentConfiguration : IEntityTypeConfiguration<ProcedureAttachment>
{
    public void Configure(EntityTypeBuilder<ProcedureAttachment> builder)
    {
        builder.ToTable("procedure_attachments", "procedures");
        builder.HasKey(a => a.Id).HasName("pk_procedure_attachments");
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(a => a.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(a => a.LabelSlug).HasColumnName("label_slug").IsRequired();
        builder.Property(a => a.FileName).HasColumnName("file_name").IsRequired();
        builder.Property(a => a.FileRef).HasColumnName("file_ref").IsRequired();
        builder.Property(a => a.ContentType).HasColumnName("content_type").IsRequired();
        builder.Property(a => a.SizeBytes).HasColumnName("size_bytes").IsRequired();
        builder.Property(a => a.UploadedBy).HasColumnName("uploaded_by").IsRequired();
        builder.Property(a => a.UploadedAt).HasColumnName("uploaded_at").HasDefaultValueSql("now()");

        builder.HasOne(a => a.Procedure).WithMany(p => p.Attachments)
            .HasForeignKey(a => a.ProcedureId)
            .HasConstraintName("fk_procedure_attachments_procedures")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => new { a.TenantId, a.ProcedureId, a.LabelSlug })
            .HasDatabaseName("ix_procedure_attachments_tenant_procedure_label");
    }
}
