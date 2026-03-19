using GamexBusinessPage.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace GamexBusinessPage.Services;

public interface IContactEmailService
{
    Task SendContactEmailAsync(ContactFormInputModel inputModel);
}

public sealed class ContactEmailService : IContactEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ContactFormSettings _contactFormSettings;
    private readonly ILogger<ContactEmailService> _logger;

    public ContactEmailService(
        IOptions<SmtpSettings> smtpOptions,
        IOptions<ContactFormSettings> contactFormOptions,
        ILogger<ContactEmailService> logger)
    {
        _smtpSettings = smtpOptions.Value;
        _contactFormSettings = contactFormOptions.Value;
        _logger = logger;
    }

    public async Task SendContactEmailAsync(ContactFormInputModel inputModel)
    {
        if (!IsConfigured())
        {
            throw new InvalidOperationException("SMTP settings are not configured.");
        }

        var subject = string.IsNullOrWhiteSpace(inputModel.Subject)
            ? "Nowa wiadomość z formularza kontaktowego"
            : inputModel.Subject.Trim();

        var plainTextBody = BuildPlainTextBody(inputModel, subject);
        var htmlBody = BuildHtmlBody(inputModel, subject);

        using var message = new MailMessage
        {
            From = new MailAddress(_smtpSettings.FromAddress, inputModel.Name),
            Subject = $"[Formularz kontaktowy] {subject}",
            Body = plainTextBody,
            IsBodyHtml = false
        };

        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(plainTextBody, Encoding.UTF8, "text/plain"));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, "text/html"));

        message.To.Add(new MailAddress(_contactFormSettings.RecipientEmail, _contactFormSettings.RecipientName));
        message.ReplyToList.Add(new MailAddress(inputModel.Email, inputModel.Name));

        using var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(_smtpSettings.UserName))
        {
            smtpClient.Credentials = new NetworkCredential(_smtpSettings.UserName, _smtpSettings.Password);
        }

        await smtpClient.SendMailAsync(message);
        _logger.LogInformation("Contact form message sent successfully to configured recipient.");
    }

    private static string BuildPlainTextBody(ContactFormInputModel inputModel, string subject)
    {
        return $"""
Nowa wiadomość z formularza kontaktowego.

Imię i nazwisko: {inputModel.Name}
E-mail: {inputModel.Email}
Telefon: {inputModel.Phone ?? "nie podano"}
Maszyna: {inputModel.Machine ?? "nie wybrano"}
Temat: {subject}

Treść wiadomości:
{inputModel.Message}
""";
    }

    private static string BuildHtmlBody(ContactFormInputModel inputModel, string subject)
    {
        var name = WebUtility.HtmlEncode(inputModel.Name);
        var email = WebUtility.HtmlEncode(inputModel.Email);
        var phone = WebUtility.HtmlEncode(inputModel.Phone ?? "nie podano");
        var machine = WebUtility.HtmlEncode(inputModel.Machine ?? "nie wybrano");
        var safeSubject = WebUtility.HtmlEncode(subject);
        var message = WebUtility.HtmlEncode(inputModel.Message).Replace("\r\n", "<br />").Replace("\n", "<br />");

        return $"""
<!DOCTYPE html>
<html lang="pl">
<body style="margin:0;padding:0;background-color:#f4f6f8;font-family:Arial,Helvetica,sans-serif;color:#1f2937;">
    <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f4f6f8;padding:24px 12px;">
        <tr>
            <td align="center">
                <table role="presentation" width="640" cellpadding="0" cellspacing="0" style="max-width:640px;width:100%;background-color:#ffffff;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;">
                    <tr>
                        <td style="background-color:#0f172a;color:#ffffff;padding:20px 24px;">
                            <h1 style="margin:0;font-size:20px;line-height:1.3;">Nowa wiadomość z formularza kontaktowego</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style="padding:24px;">
                            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;">
                                <tr>
                                    <td style="padding:8px 0;font-weight:700;width:180px;">Imię i nazwisko:</td>
                                    <td style="padding:8px 0;">{name}</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 0;font-weight:700;">E-mail:</td>
                                    <td style="padding:8px 0;">{email}</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 0;font-weight:700;">Telefon:</td>
                                    <td style="padding:8px 0;">{phone}</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 0;font-weight:700;">Maszyna:</td>
                                    <td style="padding:8px 0;">{machine}</td>
                                </tr>
                                <tr>
                                    <td style="padding:8px 0;font-weight:700;">Temat:</td>
                                    <td style="padding:8px 0;">{safeSubject}</td>
                                </tr>
                            </table>

                            <div style="margin-top:20px;padding:16px;background-color:#f9fafb;border:1px solid #e5e7eb;border-radius:8px;">
                                <p style="margin:0 0 8px 0;font-weight:700;">Treść wiadomości:</p>
                                <p style="margin:0;white-space:normal;line-height:1.6;">{message}</p>
                            </div>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>
""";
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_smtpSettings.Host)
            && _smtpSettings.Port > 0
            && !string.IsNullOrWhiteSpace(_smtpSettings.FromAddress)
            && !string.IsNullOrWhiteSpace(_contactFormSettings.RecipientEmail);
    }
}
