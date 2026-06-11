using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class QueryRuleConfiguration : IEntityTypeConfiguration<QueryRule>
{
    public void Configure(EntityTypeBuilder<QueryRule> builder)
    {
        builder.ToTable("query_rules", "procedures_config");
        builder.HasKey(r => r.Id).HasName("pk_query_rules");
        builder.Property(r => r.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(r => r.ActorDefinitionId).HasColumnName("actor_definition_id").IsRequired();
        builder.Property(r => r.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(r => r.SubjectType).HasColumnName("subject_type").IsRequired();
        builder.Property(r => r.EntryKey).HasColumnName("entry_key").IsRequired();
        builder.Property(r => r.IsBlocking).HasColumnName("is_blocking").HasDefaultValue(true);
        builder.Property(r => r.Verifications).HasColumnName("verifications")
            .HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(r => r.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(r => r.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(r => r.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(r => r.ActorDefinition).WithMany(a => a.QueryRules)
            .HasForeignKey(r => r.ActorDefinitionId)
            .HasConstraintName("fk_query_rules_actor_definitions")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => new { r.TenantId, r.ActorDefinitionId })
            .HasDatabaseName("ix_query_rules_tenant_actor");
    }
}
