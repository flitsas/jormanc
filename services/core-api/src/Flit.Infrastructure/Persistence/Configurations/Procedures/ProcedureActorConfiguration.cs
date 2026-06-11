using Flit.Infrastructure.Persistence.Entities.Procedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Procedures;

public sealed class ProcedureActorConfiguration : IEntityTypeConfiguration<ProcedureActor>
{
    public void Configure(EntityTypeBuilder<ProcedureActor> builder)
    {
        builder.ToTable("procedure_actors", "procedures");
        builder.HasKey(a => a.Id).HasName("pk_procedure_actors");
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(a => a.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(a => a.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(a => a.ActorDefinitionId).HasColumnName("actor_definition_id").IsRequired();
        builder.Property(a => a.Nature).HasColumnName("nature").IsRequired();
        builder.Property(a => a.ParentActorId).HasColumnName("parent_actor_id");
        builder.Property(a => a.DocumentType).HasColumnName("document_type");
        builder.Property(a => a.DocumentNumber).HasColumnName("document_number");
        builder.Property(a => a.Nit).HasColumnName("nit");
        builder.Property(a => a.FullName).HasColumnName("full_name");
        builder.Property(a => a.CuotaPct).HasColumnName("cuota_pct").HasColumnType("numeric(5,2)");
        builder.Property(a => a.QueryResults).HasColumnName("query_results")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(a => a.IdentityValidated).HasColumnName("identity_validated")
            .HasDefaultValue(false);
        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(a => a.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasOne(a => a.Procedure).WithMany(p => p.Actors)
            .HasForeignKey(a => a.ProcedureId)
            .HasConstraintName("fk_procedure_actors_procedures")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.ParentActor).WithMany()
            .HasForeignKey(a => a.ParentActorId)
            .HasConstraintName("fk_procedure_actors_parent")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(a => new { a.TenantId, a.ProcedureId })
            .HasDatabaseName("ix_proc_actors_procedure_id");
    }
}
