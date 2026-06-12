using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Revoca la sesión asociada al JTI del token actual (logout explícito).
/// Idempotente si la sesión ya fue revocada o no existe.
/// </summary>
public sealed class LogoutCommandHandler(
    ISessionRepository sessionRepository,
    ISessionBlacklist sessionBlacklist,
    IClock clock)
{
    public async Task HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Jti))
            return;

        var session = await sessionRepository.GetByJtiAsync(command.Jti, command.UserId, ct);
        if (session is null || session.IsRevoked)
            return;

        await sessionRepository.RevokeAsync(session, command.UserId, clock.UtcNow, ct);
        await sessionBlacklist.RevokeAsync(session.Jti, session.ExpiresAt, ct);
    }
}
