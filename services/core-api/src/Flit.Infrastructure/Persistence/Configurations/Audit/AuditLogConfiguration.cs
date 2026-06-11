using Flit.Infrastructure.Persistence.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Audit;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_log", "audit");
        builder.HasKey(a => a.Id).HasName("pk_audit_log");
        builder.Property(a => a.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(a => a.TenantId).HasColumnName("tenant_id");
        builder.Property(a => a.SchemaName).HasColumnName("schema_name").IsRequired();
        builder.Property(a => a.TableName).HasColumnName("table_name").IsRequired();
        builder.Property(a => a.RecordId).HasColumnName("record_id").IsRequired();
        builder.Property(a => a.Operation).HasColumnName("operation").IsRequired();
        builder.Property(a => a.ChangedBy).HasColumnName("changed_by");
        builder.Property(a => a.ChangedAt).HasColumnName("changed_at").HasDefaultValueSql("now()");
        builder.Property(a => a.OldValues).HasColumnName("old_values").HasColumnType("jsonb");
        builder.Property(a => a.NewValues).HasColumnName("new_values").HasColumnType("jsonb");
        builder.Property(a => a.RequestId).HasColumnName("request_id");
        builder.Property(a => a.IpAddress).HasColumnName("ip_address");

        builder.HasIndex(a => new { a.TenantId, a.TableName, a.RecordId })
            .HasDatabaseName("ix_audit_log_tenant_table_record");
        builder.HasIndex(a => a.ChangedAt).HasDatabaseName("ix_audit_log_changed_at");
    }
}
