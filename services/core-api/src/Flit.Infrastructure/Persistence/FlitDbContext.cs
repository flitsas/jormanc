using Flit.Infrastructure.Persistence.Entities.Audit;
using Flit.Infrastructure.Persistence.Entities.Companies;
using Flit.Infrastructure.Persistence.Entities.Documents;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Infrastructure.Persistence.Entities.Integrations;
using Flit.Infrastructure.Persistence.Entities.OT;
using Flit.Infrastructure.Persistence.Entities.Procedures;
using Flit.Infrastructure.Persistence.Entities.ProceduresConfig;
using Microsoft.EntityFrameworkCore;

namespace Flit.Infrastructure.Persistence;

/// <summary>
/// DbContext unificado del Modular Monolith FLIT 2.0.
/// Los DbSets cubren todos los módulos: Identity, Companies, ProceduresConfig,
/// Procedures, Documents, Integrations, OT y Audit.
/// Las configuraciones de cada entidad se aplican automáticamente vía
/// ApplyConfigurationsFromAssembly (IEntityTypeConfiguration&lt;T&gt; en este assembly).
/// </summary>
public sealed class FlitDbContext(DbContextOptions<FlitDbContext> options)
    : DbContext(options)
{
    // ── Identity ────────────────────────────────────────────────────────────
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Invitation> Invitations => Set<Invitation>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // ── Companies ────────────────────────────────────────────────────────────
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyConfig> CompanyConfigs => Set<CompanyConfig>();
    public DbSet<CompanySignatureMatrix> CompanySignatureMatrices => Set<CompanySignatureMatrix>();
    public DbSet<TenantUserException> TenantUserExceptions => Set<TenantUserException>();
    public DbSet<CompanyOtEnabled> CompanyOtEnabled => Set<CompanyOtEnabled>();

    // ── ProceduresConfig ─────────────────────────────────────────────────────
    public DbSet<ProcedureType> ProcedureTypes => Set<ProcedureType>();
    public DbSet<ProcedureStep> ProcedureSteps => Set<ProcedureStep>();
    public DbSet<FormSection> FormSections => Set<FormSection>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<ApiConnector> ApiConnectors => Set<ApiConnector>();
    public DbSet<RuleSet> RuleSets => Set<RuleSet>();
    public DbSet<ActorDefinition> ActorDefinitions => Set<ActorDefinition>();
    public DbSet<QueryRule> QueryRules => Set<QueryRule>();
    public DbSet<ProcedureTypeSnapshot> ProcedureTypeSnapshots => Set<ProcedureTypeSnapshot>();

    // ── Procedures ───────────────────────────────────────────────────────────
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<ProcedureActor> ProcedureActors => Set<ProcedureActor>();
    public DbSet<VehicleQuery> VehicleQueries => Set<VehicleQuery>();
    public DbSet<ProcedureSignature> ProcedureSignatures => Set<ProcedureSignature>();
    public DbSet<ProcedureAttachment> ProcedureAttachments => Set<ProcedureAttachment>();

    // ── Documents ────────────────────────────────────────────────────────────
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<ProcedureTypeDocument> ProcedureTypeDocuments => Set<ProcedureTypeDocument>();
    public DbSet<DocumentTemplate> DocumentTemplates => Set<DocumentTemplate>();
    public DbSet<TemplateField> TemplateFields => Set<TemplateField>();
    public DbSet<ProcedureDocument> ProcedureDocuments => Set<ProcedureDocument>();
    public DbSet<ConsolidatedPackage> ConsolidatedPackages => Set<ConsolidatedPackage>();

    // ── Integrations ─────────────────────────────────────────────────────────
    public DbSet<ConnectorConfig> ConnectorConfigs => Set<ConnectorConfig>();
    public DbSet<IntegrationLog> IntegrationLogs => Set<IntegrationLog>();
    public DbSet<IdentityValidation> IdentityValidations => Set<IdentityValidation>();

    // ── OT ───────────────────────────────────────────────────────────────────
    public DbSet<OtOrganism> OtOrganisms => Set<OtOrganism>();
    public DbSet<OtDocumentOrder> OtDocumentOrders => Set<OtDocumentOrder>();
    public DbSet<OtDocumentLabel> OtDocumentLabels => Set<OtDocumentLabel>();
    public DbSet<OtRuleSet> OtRuleSets => Set<OtRuleSet>();
    public DbSet<OtIntegrationLog> OtIntegrationLogs => Set<OtIntegrationLog>();

    // ── Audit ────────────────────────────────────────────────────────────────
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FlitDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
