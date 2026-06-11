using Flit.Infrastructure.Persistence.Entities.Procedures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Procedures;

public sealed class VehicleQueryConfiguration : IEntityTypeConfiguration<VehicleQuery>
{
    public void Configure(EntityTypeBuilder<VehicleQuery> builder)
    {
        builder.ToTable("vehicle_queries", "procedures");
        builder.HasKey(q => q.Id).HasName("pk_vehicle_queries");
        builder.Property(q => q.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(q => q.ProcedureId).HasColumnName("procedure_id").IsRequired();
        builder.Property(q => q.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(q => q.QueryKey).HasColumnName("query_key").HasDefaultValue("placa").IsRequired();
        builder.Property(q => q.QueryValue).HasColumnName("query_value").IsRequired();
        // PII: payloads de RUNT/SIMIT/RUES contienen datos personales
        builder.Property(q => q.RuntPayload).HasColumnName("runt_payload").HasColumnType("jsonb");
        builder.Property(q => q.SimitPayload).HasColumnName("simit_payload").HasColumnType("jsonb");
        builder.Property(q => q.RuesPayload).HasColumnName("rues_payload").HasColumnType("jsonb");
        builder.Property(q => q.Warnings).HasColumnName("warnings")
            .HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(q => q.QueriedAt).HasColumnName("queried_at").HasDefaultValueSql("now()");

        builder.HasOne(q => q.Procedure).WithMany(p => p.VehicleQueries)
            .HasForeignKey(q => q.ProcedureId)
            .HasConstraintName("fk_vehicle_queries_procedures")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(q => new { q.TenantId, q.ProcedureId })
            .HasDatabaseName("ix_vehicle_queries_tenant_procedure_id");
        builder.HasIndex(q => q.QueryValue)
            .HasDatabaseName("ix_vehicle_queries_query_value");
    }
}
