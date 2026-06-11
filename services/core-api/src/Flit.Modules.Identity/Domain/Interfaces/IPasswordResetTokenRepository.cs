using Flit.Infrastructure.Persistence.Entities.Identity;

namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato de repositorio de tokens de reset de contraseña (HU-9772).
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>Persiste un nuevo token de reset.</summary>
    Task CreateAsync(PasswordResetToken token, CancellationToken ct = default);

    /// <summary>
    /// Busca un token por su hash SHA-256. Incluye la navegación a User → Tenant.
    /// </summary>
    Task<PasswordResetToken?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>Persiste cambios a un token existente (used_at).</summary>
    Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default);
}
