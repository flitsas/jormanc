using Flit.Infrastructure.Persistence.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Documents;

public sealed class TemplateFieldConfiguration : IEntityTypeConfiguration<TemplateField>
{
    public void Configure(EntityTypeBuilder<TemplateField> builder)
    {
        builder.ToTable("template_fields", "documents");
        builder.HasKey(f => f.Id).HasName("pk_template_fields");
        builder.Property(f => f.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(f => f.TemplateId).HasColumnName("template_id").IsRequired();
        builder.Property(f => f.Marker).HasColumnName("marker").IsRequired();
        builder.Property(f => f.DataSource).HasColumnName("data_source").IsRequired();
        builder.Property(f => f.DataPath).HasColumnName("data_path").IsRequired();
        builder.Property(f => f.IsRequired).HasColumnName("is_required");
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(f => f.Template).WithMany(t => t.Fields)
            .HasForeignKey(f => f.TemplateId)
            .HasConstraintName("fk_template_fields_document_templates")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(f => new { f.TemplateId, f.Marker }).IsUnique()
            .HasDatabaseName("uq_template_field_marker");
    }
}
