using Flit.Infrastructure.Persistence.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Integrations;

public sealed class ConnectorConfigConfiguration : IEntityTypeConfiguration<ConnectorConfig>
{
    public void Configure(EntityTypeBuilder<ConnectorConfig> builder)
    {
        builder.ToTable("connector_configs", "integrations");
        builder.HasKey(c => c.Id).HasName("pk_connector_configs");
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(c => c.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(c => c.ConnectorType).HasColumnName("connector_type").IsRequired();
        builder.Property(c => c.Provider).HasColumnName("provider").IsRequired();
        builder.Property(c => c.CredentialsRef).HasColumnName("credentials_ref")
            .HasColumnType("jsonb").IsRequired();
        builder.Property(c => c.IsPrimary).HasColumnName("is_primary").HasDefaultValue(true);
        builder.Property(c => c.Priority).HasColumnName("priority").HasDefaultValue(1);
        builder.Property(c => c.TimeoutMs).HasColumnName("timeout_ms").HasDefaultValue(4000);
        builder.Property(c => c.IsActive).HasColumnName("is_active").HasDefaultValue(true);
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by").IsRequired();
        builder.Property(c => c.RowVersion).HasColumnName("row_version").HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.HasIndex(c => new { c.TenantId, c.ConnectorType, c.IsPrimary }).IsUnique()
            .HasFilter("is_primary = true")
            .HasDatabaseName("uq_connector_configs_tenant_type_primary");
    }
}
