using System;
using System.Net;
using System.Net.Mail;
using fluxel.Config;
using Microsoft.Extensions.Options;
using Midori.Logging;

namespace fluxel.Components;

public class MailDelivery
{
    private readonly MailConfig config;

    public MailDelivery(IOptions<MailConfig> config)
    {
        this.config = config.Value;
    }

    public bool SendMail(string to, string subject, string body)
    {
        try
        {
            var mail = new MailMessage(config.Username, to, subject, body);
            mail.From = new MailAddress(config.Username, config.Name);

            var client = new SmtpClient(config.Host, config.Port)
            {
                Credentials = new NetworkCredential(config.Username, config.Password),
                EnableSsl = config.Port == 587
            };

            client.Send(mail);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to send mail to '{to}': {ex.Message}", LoggingTarget.Network);
            return false;
        }
    }

    public static bool IsValid(string email)
    {
        try
        {
            var m = new MailAddress(email);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
