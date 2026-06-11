using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de ForgotPasswordCommand.
/// Siempre retorna success (204) para no revelar si el email existe.
/// AC3 de HU-9772.
/// </summary>
public sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    IPasswordResetTokenRepository resetTokenRepository,
    IEmailSender emailSender,
    IClock clock)
{
    private const int ExpirationHours = 24;

    public async Task HandleAsync(
        ForgotPasswordCommand command,
        CancellationToken ct = default)
    {
        var user = await userRepository.FindByEmailAndTenantSlugAsync(
            command.Email, command.TenantSlug, ct);

        // Respuesta siempre 204 para no revelar existencia del correo
        if (user is null || user.Status != "active")
            return;

        var rawToken = TokenHelper.Generate();
        var tokenHash = TokenHelper.Hash(rawToken);

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = clock.UtcNow.AddHours(ExpirationHours),
            CreatedAt = clock.UtcNow
        };

        await resetTokenRepository.CreateAsync(resetToken, ct);

        await emailSender.SendAsync(
            recipient: command.Email,
            subject: "Restablece tu contraseña — FLIT",
            htmlBody: BuildEmailBody(rawToken),
            ct: ct);
    }

    private static string BuildEmailBody(string rawToken) =>
        $"""
        <p>Recibimos una solicitud para restablecer tu contraseña.</p>
        <p>Tu token de reset es: <strong>{rawToken}</strong></p>
        <p>Úsalo en POST /api/v1/auth/reset-password con tu nueva contraseña.</p>
        <p>Este token expira en {ExpirationHours} horas. Si no solicitaste el reset, ignora este correo.</p>
        """;
}
