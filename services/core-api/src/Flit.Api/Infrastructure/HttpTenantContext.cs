using Flit.SharedKernel;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Infrastructure;

/// <summary>Resuelve tenant y usuario desde claims JWT (tid, sub).</summary>
public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst("tid")?.Value, out var tenantId)
            ? tenantId
            : Guid.Empty;

    public Guid UserId =>
        Guid.TryParse(
            httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            out var userId)
            ? userId
            : Guid.Empty;
}
