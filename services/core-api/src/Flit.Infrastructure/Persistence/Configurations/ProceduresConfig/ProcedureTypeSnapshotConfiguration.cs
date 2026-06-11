using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.ProceduresConfig;

public sealed class ProcedureTypeSnapshotConfiguration : IEntityTypeConfiguration<ProcedureTypeSnapshot>
{
    public void Configure(EntityTypeBuilder<ProcedureTypeSnapshot> builder)
    {
        builder.ToTable("procedure_type_snapshots", "procedures_config");
        builder.HasKey(s => s.Id).HasName("pk_procedure_type_snapshots");
        builder.Property(s => s.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(s => s.ProcedureTypeId).HasColumnName("procedure_type_id").IsRequired();
        builder.Property(s => s.Version).HasColumnName("version").IsRequired();
        builder.Property(s => s.SnapshotJson).HasColumnName("snapshot_json")
            .HasColumnType("jsonb").IsRequired();
        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

        builder.HasOne(s => s.ProcedureType).WithMany(t => t.Snapshots)
            .HasForeignKey(s => s.ProcedureTypeId)
            .HasConstraintName("fk_procedure_type_snapshots_procedure_types")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.ProcedureTypeId, s.Version }).IsUnique()
            .HasDatabaseName("uq_snapshot_type_version");
    }
}
