namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando: solicitar link de reset de contraseña por email.
/// AC3 de HU-9772: POST /auth/forgot-password { email, tenant_slug }
/// </summary>
public sealed record ForgotPasswordCommand(string Email, string TenantSlug);
