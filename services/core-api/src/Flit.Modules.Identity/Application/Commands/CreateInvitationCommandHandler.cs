using System.Text.Json;
using Flit.Infrastructure.Persistence.Entities.Identity;
using Flit.Modules.Identity.Application.DTOs;
using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de CreateInvitationCommand.
/// Genera token seguro, persiste hash en BD y envía email al invitado.
/// AC1 de HU-9772.
/// </summary>
public sealed class CreateInvitationCommandHandler(
    IInvitationRepository invitationRepository,
    IUserRepository userRepository,
    IEmailSender emailSender,
    IClock clock)
{
    private const int ExpirationHours = 72;

    public async Task<Result<InvitationDto, IdentityError>> HandleAsync(
        CreateInvitationCommand command,
        CancellationToken ct = default)
    {
        var emailExists = await userRepository.EmailExistsAsync(command.Email, command.TenantId, ct);
        if (emailExists)
            return Result<InvitationDto, IdentityError>.Failure(IdentityError.EmailAlreadyRegistered);

        var rawToken = TokenHelper.Generate();
        var tokenHash = TokenHelper.Hash(rawToken);

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            TenantId = command.TenantId,
            Email = command.Email,
            TokenHash = tokenHash,
            RolesJson = JsonSerializer.Serialize(command.RoleIds),
            Status = "pending",
            InvitedBy = command.InvitedBy,
            ExpiresAt = clock.UtcNow.AddHours(ExpirationHours),
            CreatedAt = clock.UtcNow
        };

        await invitationRepository.CreateAsync(invitation, ct);

        await emailSender.SendAsync(
            recipient: command.Email,
            subject: "Invitación a la plataforma FLIT",
            htmlBody: BuildEmailBody(rawToken),
            ct: ct);

        return Result<InvitationDto, IdentityError>.Success(
            new InvitationDto(invitation.Id, invitation.Email, invitation.ExpiresAt, invitation.Status));
    }

    private static string BuildEmailBody(string rawToken) =>
        $"""
        <p>Has sido invitado a unirte a la plataforma FLIT.</p>
        <p>Tu token de invitación es: <strong>{rawToken}</strong></p>
        <p>Usa este token en POST /api/v1/invitations/{rawToken}/accept para completar tu registro.</p>
        <p>Este enlace expira en {ExpirationHours} horas.</p>
        """;
}
