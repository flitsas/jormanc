using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Flit.Infrastructure.Persistence;

/// <summary>
/// Activa variables de sesión PostgreSQL para RLS al abrir conexión física nueva.
/// </summary>
public sealed class TenantConnectionInterceptor(IHttpContextAccessor httpContextAccessor)
    : DbConnectionInterceptor
{
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await TenantSessionContext.ApplyAsync(connection, httpContextAccessor, cancellationToken);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        TenantSessionContext.Apply(connection, httpContextAccessor);
        base.ConnectionOpened(connection, eventData);
    }
}
