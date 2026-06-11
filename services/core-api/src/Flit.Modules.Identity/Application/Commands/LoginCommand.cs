namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando: autenticar usuario con email + password + tenant_slug.
/// AC1 y AC2 de HU-9769.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    string TenantSlug);
