using Flit.Infrastructure.Persistence;
using Flit.Infrastructure.Persistence.SeedData;
using Flit.Modules.Rbac.Ports;
using Flit.Modules.Users.Adapters;
using Flit.Modules.Users.Domain;
using Flit.Modules.Users.Ports;
using Flit.SharedKernel;
using Npgsql;

namespace Flit.Api.Endpoints;

/// <summary>
/// Endpoints SOLO para entorno Development. Permiten levantar la demo local
/// sin AWS Cognito ni Verifik.
///
/// Requiere migraciones EF aplicadas (<c>pnpm migrate</c> o <c>MigrateAsync</c> al arrancar).
/// Si faltan roles/permisos, aplica FlitV2Seeds antes de asignar ADMIN.
///
/// Endpoint: POST /api/v1/dev/seed-admin?email=admin@flit.io
/// Idempotente: si ya existe, garantiza el rol ADMIN.
/// </summary>
public static class DevSeedEndpoints
{
    public static void MapDevSeedEndpoints(this IEndpointRouteBuilder app, IWebHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        app.MapPost("/api/v1/dev/seed-admin", async (
            string? email,
            FlitDbContext db,
            IUsersRepository usersRepo,
            IRolesRepository rolesRepo,
            IRoleAssignmentsRepository assignRepo,
            IPermissionsCache cache,
            IClock clock,
            CancellationToken ct) =>
        {
            var targetEmail = (email ?? "admin@flit.io").Trim().ToLowerInvariant();
            var sub = StubCognitoDirectory.SubForEmail(targetEmail);

            // 1. Usuario
            var user = await usersRepo.GetByEmailAsync(targetEmail, ct);
            if (user is null)
            {
                user = User.Create(
                    email: targetEmail,
                    fullName: "Administrador Demo",
                    documentType: "CC",
                    documentNumber: "1000000000",
                    phone: "3000000000",
                    createdByUserId: null,
                    now: clock.UtcNow);
                user.LinkCognitoSub(sub, clock.UtcNow);
                await usersRepo.AddAsync(user, ct);
            }

            // 2. Rol ADMIN (seeds SQL o dev endpoint)
            var adminRole = await rolesRepo.GetByCodeAsync("ADMIN", ct);
            if (adminRole is null)
            {
                try
                {
                    await FlitShellSeedData.ApplyAllAsync(db, ct);
                    adminRole = await rolesRepo.GetByCodeAsync("ADMIN", ct);
                }
                catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UndefinedTable)
                {
                    return Results.Problem(
                        "Schema no encontrado. Ejecuta pnpm migrate o levanta core-api con ConnectionStrings:Core.",
                        statusCode: 500);
                }
            }

            if (adminRole is null)
            {
                return Results.Problem(
                    "Rol ADMIN no encontrado tras aplicar seeds. Verifica migraciones EF y FlitV2Seeds.",
                    statusCode: 500);
            }

            // 3. Assignment user -> ADMIN (idempotente)
            var currentRoles = await rolesRepo.GetByUserAsync(user.Id, ct);
            if (currentRoles.All(r => r.Id != adminRole.Id))
            {
                await assignRepo.AssignRoleToUserAsync(
                    user.Id, adminRole.Id, assignedByUserId: null, ct);
            }

            // 4. Invalidar cache de permisos
            await cache.InvalidateAsync(user.Id, ct);

            return Results.Ok(new
            {
                userId = user.Id,
                email = user.Email,
                cognitoSub = sub,
                role = "ADMIN",
                message = "Usuario admin listo. Login con cualquier password " +
                          "(StubCognitoDirectory acepta todas en dev).",
            });
        })
        .WithName("DevSeedAdmin")
        .WithTags("Dev");
    }
}
