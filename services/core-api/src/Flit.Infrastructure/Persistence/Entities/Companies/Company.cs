using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Infrastructure.Persistence.Entities.Companies;

/// <summary>
/// Compañía / Organismo de Tránsito cliente del SaaS.
/// Una compañía = un tenant (relación 1:1).
/// schema: companies / tabla: companies
/// </summary>
public sealed class Company
{
    public Guid Id { get; set; }
    /// <summary>FK a identity.tenants (relación 1:1).</summary>
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Nit { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    /// <summary>active | inactive | suspended</summary>
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }
    public int RowVersion { get; set; }

    public CompanyConfig? Config { get; set; }
    public ICollection<CompanySignatureMatrix> SignatureMatrix { get; set; } = [];
    public ICollection<TenantUserException> UserExceptions { get; set; } = [];
    public ICollection<CompanyOtEnabled> OtEnabled { get; set; } = [];
}
