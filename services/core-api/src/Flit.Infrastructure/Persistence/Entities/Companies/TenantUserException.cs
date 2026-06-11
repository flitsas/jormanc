namespace Flit.Infrastructure.Persistence.Entities.Companies;

/// <summary>
/// Lista blanca de usuarios eximidos del interceptor only_own_vehicles.
/// schema: companies / tabla: tenant_user_exceptions
/// </summary>
#pragma warning disable CA1711
public sealed class TenantUserException
#pragma warning restore CA1711
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    /// <summary>FK a identity.users.</summary>
    public Guid UserId { get; set; }
    public Guid? AddedBy { get; set; }
    public DateTimeOffset AddedAt { get; set; }

    public Company Company { get; set; } = null!;
}
