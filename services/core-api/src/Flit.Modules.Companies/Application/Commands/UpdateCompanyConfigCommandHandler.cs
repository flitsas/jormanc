using Flit.Modules.Companies.Application.DTOs;
using Flit.Modules.Companies.Domain.Errors;
using Flit.Modules.Companies.Domain.Interfaces;
using Flit.SharedKernel;

namespace Flit.Modules.Companies.Application.Commands;

/// <summary>
/// Handler de UpdateCompanyConfigCommand.
/// Valida dominios (notification_target, smtp_mode) y persiste la config.
/// AC3 HU-9774.
/// </summary>
public sealed class UpdateCompanyConfigCommandHandler(
    ICompanyRepository companyRepository,
    IClock clock)
{
    private static readonly HashSet<string> ValidNotificationTargets =
        ["comprador", "radicador", "ninguno"];

    private static readonly HashSet<string> ValidSmtpModes =
        ["native", "api_cliente"];

    public async Task<Result<CompanyConfigDto, CompanyError>> HandleAsync(
        UpdateCompanyConfigCommand command, CancellationToken ct = default)
    {
        if (!ValidNotificationTargets.Contains(command.NotificationTarget))
            return Result<CompanyConfigDto, CompanyError>.Failure(CompanyError.InvalidNotificationTarget);

        if (!ValidSmtpModes.Contains(command.SmtpMode))
            return Result<CompanyConfigDto, CompanyError>.Failure(CompanyError.InvalidSmtpMode);

        var company = await companyRepository.FindByIdWithConfigAsync(command.CompanyId, ct);

        if (company is null)
            return Result<CompanyConfigDto, CompanyError>.Failure(CompanyError.NotFound);

        if (company.Config is null)
            return Result<CompanyConfigDto, CompanyError>.Failure(CompanyError.ConfigNotFound);

        var config = company.Config;
        config.OnlyOwnVehicles = command.OnlyOwnVehicles;
        config.BaulFirmasEnabled = command.BaulFirmasEnabled;
        config.NotificationTarget = command.NotificationTarget;
        config.SmtpMode = command.SmtpMode;
        config.MatriculaConfig = command.MatriculaConfig;
        config.TraspasosConfig = command.TraspasosConfig;
        config.ContingencyConfig = command.ContingencyConfig;
        config.RecaudoMethods = command.RecaudoMethods;
        config.UpdatedAt = clock.UtcNow;

        await companyRepository.UpdateConfigAsync(config, ct);

        return Result<CompanyConfigDto, CompanyError>.Success(new CompanyConfigDto(
            Id: config.Id,
            CompanyId: config.CompanyId,
            OnlyOwnVehicles: config.OnlyOwnVehicles,
            BaulFirmasEnabled: config.BaulFirmasEnabled,
            NotificationTarget: config.NotificationTarget,
            SmtpMode: config.SmtpMode,
            MatriculaConfig: config.MatriculaConfig,
            TraspasosConfig: config.TraspasosConfig,
            ContingencyConfig: config.ContingencyConfig,
            RecaudoMethods: config.RecaudoMethods));
    }
}
