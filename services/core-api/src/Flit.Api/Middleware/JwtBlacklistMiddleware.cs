using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Middleware;

/// <summary>
/// Verifica que el JTI del token no esté en la blacklist (ADR-0013).
/// Se ejecuta DESPUÉS de que el middleware de autenticación JWT valida la firma.
/// Retorna 403 con { error: "session_revoked" } si el token fue revocado.
/// </summary>
public sealed class JwtBlacklistMiddleware(ISessionBlacklist blacklist) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var jti = context.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (jti is not null && await blacklist.IsRevokedAsync(jti, context.RequestAborted))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new { error = "session_revoked" },
                context.RequestAborted);
            return;
        }

        await next(context);
    }
}
