using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio de invitaciones para el módulo Identity (HU-9772).
/// </summary>
public interface IInvitationRepository
{
    /// <summary>Persiste una nueva invitación.</summary>
    Task CreateAsync(Invitation invitation, CancellationToken ct = default);

    /// <summary>
    /// Busca una invitación por el hash SHA-256 del token.
    /// Incluye la navegación a Tenant.
    /// </summary>
    Task<Invitation?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Persiste cambios a una invitación existente (status, accepted_at).</summary>
    Task UpdateAsync(Invitation invitation, CancellationToken ct = default);
}
