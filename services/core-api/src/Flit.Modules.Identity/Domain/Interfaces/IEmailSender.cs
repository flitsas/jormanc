namespace Flit.Modules.Identity.Domain.Interfaces;

/// <summary>
/// Contrato para envío de correos. En dev: ConsoleEmailSender (log a consola).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default);
}
