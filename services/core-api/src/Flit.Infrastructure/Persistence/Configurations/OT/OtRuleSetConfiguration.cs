using Flit.Infrastructure.Persistence.Entities.OT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.OT;

public sealed class OtRuleSetConfiguration : IEntityTypeConfiguration<OtRuleSet>
{
    public void Configure(EntityTypeBuilder<OtRuleSet> builder)
    {
        builder.ToTable("ot_rule_sets", "ot");
        builder.HasKey(r => r.Id).HasName("pk_ot_rule_sets");
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(r => r.OtId).HasColumnName("ot_id").IsRequired();
        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.Name).HasColumnName("name").IsRequired();
        builder.Property(r => r.Conditions).HasColumnName("conditions").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.Actions).HasColumnName("actions").HasColumnType("jsonb").IsRequired();
        builder.Property(r => r.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(r => r.Version).HasColumnName("version").HasDefaultValue(1);
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(r => r.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(r => r.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(r => r.OtOrganism).WithMany(o => o.RuleSets)
            .HasForeignKey(r => r.OtId)
            .HasConstraintName("fk_ot_rule_sets_organisms")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.OtId, r.IsActive })
            .HasDatabaseName("ix_ot_rule_sets_tenant_ot_active");
    }
}
