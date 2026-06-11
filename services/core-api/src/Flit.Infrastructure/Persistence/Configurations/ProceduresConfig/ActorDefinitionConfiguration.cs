using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class ActorDefinitionConfiguration : IEntityTypeConfiguration<ActorDefinition>
{
    public void Configure(EntityTypeBuilder<ActorDefinition> builder)
    {
        builder.ToTable("actor_definitions", "procedures_config");
        builder.HasKey(a => a.Id).HasName("pk_actor_definitions");
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(a => a.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(a => a.Role).HasColumnName("role").IsRequired();
        builder.Property(a => a.AllowedNature).HasColumnName("allowed_nature").HasDefaultValue("ambas");
        builder.Property(a => a.MinCount).HasColumnName("min_count").HasDefaultValue(1);
        builder.Property(a => a.MaxCount).HasColumnName("max_count").HasDefaultValue(1);
        builder.Property(a => a.IsRequired).HasColumnName("is_required").HasDefaultValue(true);
        builder.Property(a => a.OrderIndex).HasColumnName("order_index").HasDefaultValue(0);
        builder.Property(a => a.LegalRepActorId).HasColumnName("legal_rep_actor_id");
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(a => a.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(a => a.ProcedureType).WithMany(t => t.ActorDefinitions)
            .HasForeignKey(a => a.ProcedureTypeId)
            .HasConstraintName("fk_actor_definitions_procedure_types")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.LegalRepActor).WithMany()
            .HasForeignKey(a => a.LegalRepActorId)
            .HasConstraintName("fk_actor_definitions_legal_rep")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.TenantId, a.ProcedureTypeId })
            .HasDatabaseName("ix_actor_definitions_tenant_type");
    }
}
