namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato para notificar al cliente (front-end) que una sesión fue revocada.
/// La implementación de producción usa SignalR (ADR-0013).
/// Inyectada en AssignUserRolesCommandHandler para desacoplar la capa de
/// aplicación de la infraestructura de transporte en tiempo real.
/// </summary>
public interface ISessionNotifier
{
    /// <summary>
    /// Notifica al usuario afectado que su sesión fue revocada.
    /// </summary>
    /// <param name="userId">ID del usuario cuya sesión fue revocada.</param>
    /// <param name="reason">Motivo de la revocación (p. ej. "roles_changed").</param>
    Task NotifySessionRevokedAsync(Guid userId, string reason, CancellationToken ct = default);
}
