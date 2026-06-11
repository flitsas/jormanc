using Flit.Modules.Identity.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Flit.Modules.Identity.Infrastructure.Email;

/// <summary>
/// Implementación de desarrollo de IEmailSender.
/// Escribe los emails a consola y los almacena en memoria para tests de integración.
/// No envía correos reales; solo para ambientes dev/test.
/// </summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    private static readonly Action<ILogger, string, string, string, Exception?> LogEmail =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Information,
            new EventId(1, "DevEmail"),
            "[DEV-EMAIL] To={Recipient} | Subject={Subject} | Body={Body}");

    /// <summary>
    /// Emails enviados durante la sesión (útil para tests en memoria).
    /// </summary>
    public List<SentEmail> SentEmails { get; } = [];

    public Task SendAsync(string recipient, string subject, string htmlBody, CancellationToken ct = default)
    {
        var email = new SentEmail(recipient, subject, htmlBody, DateTimeOffset.UtcNow);
        SentEmails.Add(email);
        LogEmail(logger, recipient, subject, htmlBody, null);
        return Task.CompletedTask;
    }
}

/// <summary>Email enviado registrado en memoria (solo dev).</summary>
public sealed record SentEmail(
    string To,
    string Subject,
    string HtmlBody,
    DateTimeOffset SentAt);
