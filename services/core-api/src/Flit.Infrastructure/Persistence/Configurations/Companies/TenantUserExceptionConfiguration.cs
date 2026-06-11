using Flit.Infrastructure.Persistence.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Companies;

public sealed class TenantUserExceptionConfiguration : IEntityTypeConfiguration<TenantUserException>
{
    public void Configure(EntityTypeBuilder<TenantUserException> builder)
    {
        builder.ToTable("tenant_user_exceptions", "companies");
        builder.HasKey(e => e.Id).HasName("pk_tenant_user_exceptions");
        builder.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(e => e.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.AddedBy).HasColumnName("added_by");
        builder.Property(e => e.AddedAt).HasColumnName("added_at").HasDefaultValueSql("now()");

        builder.HasOne(e => e.Company).WithMany(c => c.UserExceptions)
            .HasForeignKey(e => e.CompanyId)
            .HasConstraintName("fk_tenant_user_exceptions_companies")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.CompanyId, e.UserId }).IsUnique()
            .HasDatabaseName("uq_tenant_user_exception");
        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_tenant_user_exceptions_user_id");
    }
}
