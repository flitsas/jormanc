using Flit.Infrastructure.Persistence.Entities.OT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.OT;

public sealed class OtDocumentOrderConfiguration : IEntityTypeConfiguration<OtDocumentOrder>
{
    public void Configure(EntityTypeBuilder<OtDocumentOrder> builder)
    {
        builder.ToTable("ot_document_orders", "ot");
        builder.HasKey(o => o.Id).HasName("pk_ot_document_orders");
        builder.Property(o => o.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(o => o.OtId).HasColumnName("ot_id").IsRequired();
        builder.Property(o => o.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(o => o.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(o => o.OrderedDocumentTypeIds).HasColumnName("ordered_document_type_ids")
            .HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(o => o.OtOrganism).WithMany(org => org.DocumentOrders)
            .HasForeignKey(o => o.OtId)
            .HasConstraintName("fk_ot_doc_orders_organisms")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.TenantId, o.OtId, o.ProcedureTypeId }).IsUnique()
            .HasDatabaseName("uq_ot_doc_orders_tenant_ot_type");
    }
}
