using Flit.Infrastructure.Persistence.Entities.OT;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.OT;

public sealed class OtIntegrationLogConfiguration : IEntityTypeConfiguration<OtIntegrationLog>
{
    public void Configure(EntityTypeBuilder<OtIntegrationLog> builder)
    {
        builder.ToTable("ot_integration_logs", "ot");
        builder.HasKey(l => l.Id).HasName("pk_ot_integration_logs");
        builder.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(l => l.OtId).HasColumnName("ot_id").IsRequired();
        builder.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(l => l.EventType).HasColumnName("event_type").IsRequired();
        builder.Property(l => l.ProcedureRef).HasColumnName("procedure_ref");
        builder.Property(l => l.RequestPayload).HasColumnName("request_payload").HasColumnType("jsonb");
        builder.Property(l => l.ResponsePayload).HasColumnName("response_payload").HasColumnType("jsonb");
        builder.Property(l => l.HttpStatus).HasColumnName("http_status");
        builder.Property(l => l.DurationMs).HasColumnName("duration_ms");
        builder.Property(l => l.LoggedAt).HasColumnName("logged_at").HasDefaultValueSql("now()");

        builder.HasOne(l => l.OtOrganism).WithMany(o => o.IntegrationLogs)
            .HasForeignKey(l => l.OtId)
            .HasConstraintName("fk_ot_integration_logs_organisms")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => new { l.TenantId, l.OtId })
            .HasDatabaseName("ix_ot_integration_logs_tenant_ot");
        // A11: tenant_id como primera columna
        builder.HasIndex(l => new { l.TenantId, l.LoggedAt }).HasDatabaseName("ix_ot_integration_logs_tenant_logged_at");
    }
}
