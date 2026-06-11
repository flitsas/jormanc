namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Comando: aplicar nuevo password con token de reset.
/// AC3 de HU-9772: POST /auth/reset-password { token, new_password }
/// </summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword);
