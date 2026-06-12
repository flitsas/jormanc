using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Flit.Api.Hubs;

/// <summary>
/// SignalR resuelve Clients.User() desde el claim JWT <c>sub</c> (MapInboundClaims=false).
/// </summary>
public sealed class JwtSubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
        => connection.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
