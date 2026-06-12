namespace Flit.Modules.Identity.Application.Commands;

/// <summary>Cierra la sesión JWT actual (revoca JTI en BD + blacklist).</summary>
public sealed record LogoutCommand(Guid UserId, string Jti);
