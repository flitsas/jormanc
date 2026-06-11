using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class RuleSetConfiguration : IEntityTypeConfiguration<RuleSet>
{
    public void Configure(EntityTypeBuilder<RuleSet> builder)
    {
        builder.ToTable("rule_sets", "procedures_config");
        builder.HasKey(r => r.Id).HasName("pk_rule_sets");
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(r => r.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.Conditions).HasColumnName("conditions").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.Actions).HasColumnName("actions").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(r => r.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(r => r.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(r => r.ProcedureType).WithMany(t => t.RuleSets)
            .HasForeignKey(r => r.ProcedureTypeId)
            .HasConstraintName("fk_rule_sets_procedure_types")
            .OnDelete(DeleteBehavior.Cascade);

        // GIN index para evaluación de reglas
        builder.HasIndex(r => r.Conditions).HasMethod("gin")
            .HasDatabaseName("ix_rule_sets_conditions_gin");
        builder.HasIndex(r => new { r.TenantId, r.ProcedureTypeId, r.IsActive })
            .HasDatabaseName("ix_rule_sets_tenant_type_active");
    }
}
