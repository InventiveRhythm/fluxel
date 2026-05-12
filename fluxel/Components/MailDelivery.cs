using System;
using System.Net;
using System.Net.Mail;
using fluxel.Config;
using Midori.Logging;

namespace fluxel.Components;

public class MailDelivery
{
    private readonly ServerConfig config;

    public MailDelivery(ServerConfig config)
    {
        this.config = config;
    }

    public bool SendMail(string to, string subject, string body, long id, bool asFlux = false)
    {
        try
        {
            var settings = asFlux ? config.MailFlux : config.Mail;

            var mail = new MailMessage(settings.Username, to, subject, body);
            mail.From = new MailAddress(settings.Username, settings.Name);

            var client = new SmtpClient(settings.Host, settings.Port)
            {
                Credentials = new NetworkCredential(settings.Username, settings.Password),
                EnableSsl = settings.Port == 587
            };

            client.Send(mail);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to send mail to {id} ({to}): {ex.Message}", LoggingTarget.Network);
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
