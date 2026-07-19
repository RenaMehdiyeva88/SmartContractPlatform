using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using SmartContractPlatform.BLL.Services.Interfaces;
using SmartContractPlatform.DAL.Entities;

namespace SmartContractPlatform.BLL.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{
    public Task SendContractInvitationAsync(Contract contract, User recipient, CancellationToken cancellationToken = default)
        => SendAsync(recipient.Email, "Contract invitation", $"You have been invited to review and sign contract {contract.Id}.", cancellationToken);

    public Task SendUnregisteredInvitationAsync(Contract contract, string? contactEmail, string? contactPhone, CancellationToken cancellationToken = default)
    {
        // Qeyd: real SMS inteqrasiyası (məs. ASAN, digər provayder) hələ bağlanmayıb.
        // Hazırda yalnız email göndərilir, contactPhone loglanır ki, gələcəkdə SMS servisi asanlıqla qoşulsun.
        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            return Task.CompletedTask;
        }

        var registerUrl = configuration["Frontend:RegisterUrl"] ?? "https://app.smartcontractplatform.local/register";
        var body = $"Sizi '{contract.Id}' nömrəli müqaviləni imzalamaq üçün dəvət edirlər. " +
                   $"Davam etmək üçün qeydiyyatdan keçin: {registerUrl}?contractRef={contract.Id}";

        return SendAsync(contactEmail, "Müqavilə dəvəti", body, cancellationToken);
    }

    public Task SendContractSignedAsync(Contract contract, User recipient, CancellationToken cancellationToken = default)
        => SendAsync(recipient.Email, "Contract signed", $"A signature was added to contract {contract.Id}. Current status: {contract.Status}.", cancellationToken);

    public Task SendContractRejectedAsync(Contract contract, User recipient, string reason, CancellationToken cancellationToken = default)
        => SendAsync(recipient.Email, "Contract rejected", $"Contract {contract.Id} was rejected. Reason: {reason}", cancellationToken);

    private async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var host = configuration["Smtp:Host"];
        var mockEnabled = bool.TryParse(configuration["Smtp:MockEnabled"], out var configuredMockEnabled) && configuredMockEnabled;

        if (string.IsNullOrWhiteSpace(to))
        {
            throw new InvalidOperationException("Email recipient is empty.");
        }

        if (mockEnabled || string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning(
                "SMTP is not configured or mock mode is enabled. Email was not sent. To: {To}; Subject: {Subject}; Body: {Body}",
                to,
                subject,
                body);
            return;
        }

        var from = configuration["Smtp:FromEmail"] ?? configuration["Smtp:From"];
        if (string.IsNullOrWhiteSpace(from))
        {
            throw new InvalidOperationException("SMTP FromEmail is not configured.");
        }

        var port = int.TryParse(configuration["Smtp:Port"], out var configuredPort) ? configuredPort : 587;
        var useSsl = bool.TryParse(configuration["Smtp:UseSsl"], out var configuredUseSsl) && configuredUseSsl;
        var username = configuration["Smtp:Username"];
        var password = configuration["Smtp:Password"];

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port, useSsl, cancellationToken);

        if (!string.IsNullOrWhiteSpace(username))
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("SMTP Password is required when Username is configured.");
            }

            await client.AuthenticateAsync(username, password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        logger.LogInformation("Email sent. To: {To}; Subject: {Subject}", to, subject);
    }
}