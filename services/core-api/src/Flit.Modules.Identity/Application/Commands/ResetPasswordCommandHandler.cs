using Flit.Modules.Identity.Domain.Errors;
using Flit.Modules.Identity.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Identity.Application.Commands;

/// <summary>
/// Handler de ResetPasswordCommand.
/// Verifica token, actualiza hash del password y marca el token como usado.
/// Segundo uso del mismo token → RESET_TOKEN_ALREADY_USED (AC3 HU-9772).
/// </summary>
public sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository resetTokenRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IClock clock)
{
    public async Task<Result<bool, IdentityError>> HandleAsync(
        ResetPasswordCommand command,
        CancellationToken ct = default)
    {
        var tokenHash = TokenHelper.Hash(command.Token);
        var resetToken = await resetTokenRepository.FindByTokenHashAsync(tokenHash, ct);

        if (resetToken is null)
            return Result<bool, IdentityError>.Failure(IdentityError.ResetTokenNotFound);

        if (resetToken.ExpiresAt <= clock.UtcNow)
            return Result<bool, IdentityError>.Failure(IdentityError.ResetTokenExpired);

        if (resetToken.UsedAt is not null)
            return Result<bool, IdentityError>.Failure(IdentityError.ResetTokenAlreadyUsed);

        var user = resetToken.User;
        user.PasswordHash = passwordHasher.Hash(command.NewPassword);
        user.MustResetPwd = false;
        user.UpdatedAt = clock.UtcNow;

        resetToken.UsedAt = clock.UtcNow;

        await resetTokenRepository.UpdateAsync(resetToken, ct);
        await userRepository.UpdateAsync(ct);

        return Result<bool, IdentityError>.Success(true);
    }
}
