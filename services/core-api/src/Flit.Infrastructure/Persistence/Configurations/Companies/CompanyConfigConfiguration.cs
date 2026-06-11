using Flit.Infrastructure.Persistence.Entities.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Flit.Infrastructure.Persistence.Configurations.Companies;

public sealed class CompanyConfigConfiguration : IEntityTypeConfiguration<CompanyConfig>
{
    public void Configure(EntityTypeBuilder<CompanyConfig> builder)
    {
        builder.ToTable("company_configs", "companies");
        builder.HasKey(c => c.Id).HasName("pk_company_configs");
        builder.Property(c => c.Id).HasColumnName("id").HasDefaultValueSql("uuidv7()");
        builder.Property(c => c.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(c => c.OnlyOwnVehicles).HasColumnName("only_own_vehicles").HasDefaultValue(false);
        builder.Property(c => c.BaulFirmasEnabled).HasColumnName("baul_firmas_enabled").HasDefaultValue(false);
        builder.Property(c => c.NotificationTarget).HasColumnName("notification_target")
            .HasDefaultValue("radicador").IsRequired();
        builder.Property(c => c.SmtpMode).HasColumnName("smtp_mode")
            .HasDefaultValue("native").IsRequired();
        builder.Property(c => c.SmtpConfigEncrypted).HasColumnName("smtp_config_encrypted");
        builder.Property(c => c.MatriculaConfig).HasColumnName("matricula_config")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.TraspasosConfig).HasColumnName("traspasos_config")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.ContingencyConfig).HasColumnName("contingency_config")
            .HasColumnType("jsonb").HasDefaultValue("{}");
        builder.Property(c => c.RecaudoMethods).HasColumnName("recaudo_methods")
            .HasColumnType("jsonb").HasDefaultValue("[]");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");

        builder.HasOne(c => c.Company).WithOne(co => co.Config)
            .HasForeignKey<CompanyConfig>(c => c.CompanyId)
            .HasConstraintName("fk_company_configs_companies")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CompanyId).IsUnique().HasDatabaseName("uq_company_configs_company_id");
    }
}
