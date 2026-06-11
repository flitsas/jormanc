using Flit.Infrastructure.Persistence.Entities.Procedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Procedures;

public sealed class ProcedureConfiguration : IEntityTypeConfiguration<Procedure>
{
    public void Configure(EntityTypeBuilder<Procedure> builder)
    {
        builder.ToTable("procedures", "procedures");
        builder.HasKey(p => p.Id).HasName("pk_procedures");
        builder.Property(p => p.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(p => p.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(p => p.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(p => p.OtId).HasColumnName("ot_id");
        builder.Property(p => p.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(p => p.ProcedureTypeSnapshotId).HasColumnName("procedure_type_snapshot_id")
            .IsRequired();
        builder.Property(p => p.CompositeId).HasColumnName("composite_id").IsRequired();
        builder.Property(p => p.Status).HasColumnName("status").HasDefaultValue("draft").IsRequired();
        builder.Property(p => p.CurrentStepOrder).HasColumnName("current_step_order").HasDefaultValue(1);
        builder.Property(p => p.StepData).HasColumnName("step_data")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(p => p.AssignedUserId).HasColumnName("assigned_user_id");
        builder.Property(p => p.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(p => p.ApprovedAt).HasColumnName("approved_at");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");
        builder.Property(p => p.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasIndex(p => p.CompositeId).IsUnique().HasDatabaseName("uq_procedures_composite_id");
        builder.HasIndex(p => new { p.TenantId, p.Status }).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_procedures_tenant_status");
        builder.HasIndex(p => new { p.TenantId, p.CompanyId }).HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_procedures_tenant_company_id");
        // A11: tenant_id como primera columna
        builder.HasIndex(p => new { p.TenantId, p.SubmittedAt }).HasDatabaseName("ix_procedures_tenant_submitted_at");

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
