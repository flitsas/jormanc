using Flit.Infrastructure.Persistence.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Integrations;

public sealed class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
{
    public void Configure(EntityTypeBuilder<IntegrationLog> builder)
    {
        builder.ToTable("integration_logs", "integrations");
        builder.HasKey(l => l.Id).HasName("pk_integration_logs");
        builder.Property(l => l.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(l => l.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(l => l.ConnectorType).HasColumnName("connector_type").IsRequired();
        builder.Property(l => l.Operation).HasColumnName("operation").IsRequired();
        builder.Property(l => l.Provider).HasColumnName("provider").IsRequired();
        builder.Property(l => l.RequestPayload).HasColumnName("request_payload").HasColumnType("jsonb");
        builder.Property(l => l.ResponsePayload).HasColumnName("response_payload").HasColumnType("jsonb");
        builder.Property(l => l.HttpStatus).HasColumnName("http_status");
        builder.Property(l => l.DurationMs).HasColumnName("duration_ms").HasDefaultValue(0);
        builder.Property(l => l.ErrorMessage).HasColumnName("error_message");
        builder.Property(l => l.LoggedAt).HasColumnName("logged_at").HasDefaultValueSql("now()");

        builder.HasIndex(l => new { l.TenantId, l.ConnectorType })
            .HasDatabaseName("ix_integration_logs_tenant_type");
        // A11: tenant_id como primera columna
        builder.HasIndex(l => new { l.TenantId, l.LoggedAt }).HasDatabaseName("ix_integration_logs_tenant_logged_at");
    }
}
