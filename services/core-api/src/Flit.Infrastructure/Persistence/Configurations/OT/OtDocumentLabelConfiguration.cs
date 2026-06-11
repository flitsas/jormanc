using Flit.Infrastructure.Persistence.Entities.OT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.OT;

public sealed class OtDocumentLabelConfiguration : IEntityTypeConfiguration<OtDocumentLabel>
{
    public void Configure(EntityTypeBuilder<OtDocumentLabel> builder)
    {
        builder.ToTable("ot_document_labels", "ot");
        builder.HasKey(l => l.Id).HasName("pk_ot_document_labels");
        builder.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(l => l.OtId).HasColumnName("ot_id").IsRequired();
        builder.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(l => l.Slug).HasColumnName("slug").IsRequired();
        builder.Property(l => l.DisplayName).HasColumnName("display_name").IsRequired();
        builder.Property(l => l.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(l => l.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(l => l.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(l => l.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(l => l.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(l => l.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(l => l.OtOrganism).WithMany(o => o.DocumentLabels)
            .HasForeignKey(l => l.OtId)
            .HasConstraintName("fk_ot_doc_labels_organisms")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.OtId, l.Slug }).IsUnique()
            .HasDatabaseName("uq_ot_doc_label_ot_slug");
    }
}
