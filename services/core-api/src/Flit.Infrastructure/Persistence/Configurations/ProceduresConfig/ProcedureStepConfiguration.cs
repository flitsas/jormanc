using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class ProcedureStepConfiguration : IEntityTypeConfiguration<ProcedureStep>
{
    public void Configure(EntityTypeBuilder<ProcedureStep> builder)
    {
        builder.ToTable("procedure_steps", "procedures_config");
        builder.HasKey(s => s.Id).HasName("pk_procedure_steps");
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(s => s.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(s => s.OrderIndex).HasColumnName("order_index").IsRequired();
        builder.Property(s => s.Name).HasColumnName("name").IsRequired();
        builder.Property(s => s.StepType).HasColumnName("step_type").HasDefaultValue("form").IsRequired();
        builder.Property(s => s.IsRequired).HasColumnName("is_required").HasDefaultValue(true);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(s => s.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(s => s.ProcedureType).WithMany(t => t.Steps)
            .HasForeignKey(s => s.ProcedureTypeId)
            .HasConstraintName("fk_procedure_steps_procedure_types")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TenantId, s.ProcedureTypeId, s.OrderIndex })
            .HasDatabaseName("ix_procedure_steps_tenant_type_order");
    }
}
