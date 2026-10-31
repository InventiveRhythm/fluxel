using System;
using System.Text;
using fluxel.Models;
using fluxel.Models.Monetize;

namespace fluxel.Components;

public class Donations
{
    private readonly DatabaseContext database;
    private readonly MailDelivery mail;

    public Donations(DatabaseContext database, MailDelivery mail)
    {
        this.database = database;
        this.mail = mail;
    }

    public void RegisterPayment(KoFiPayment payment)
    {
        using (database.EditAndSave())
        {
            payment.RedemptionCode = new DonatorRedemptionCode
            {
                UsesMaximum = 1,
                DonateTime = TimeSpan.FromDays(payment.Amount / 2 * 31)
            };

            database.KoFiPayments.Add(payment);
        }

        sendCodeLink(payment.Email, payment.Amount, payment.RedemptionCode.Code);
    }

    private void sendCodeLink(string email, double amount, string code)
    {
        var span = TimeSpan.FromDays(amount / 2d * 31);
        var months = span.Days / 31;
        var str = "";

        if (months >= 1f)
        {
            str += $"{months} month{(months > 1 ? "s" : "")}";

            var rest = span.Days - months * 31;
            if (rest > 0) str += $" and {rest} day{(rest > 1 ? "s" : "")}";
        }
        else
            str = $"{span.Days} days";

        var body = new StringBuilder();
        body.AppendLine("Hi!");
        body.AppendLine();
        body.AppendLine("Thank you for supporting fluXis!");
        body.AppendLine("The game runs without ads or forced payments because of people like you.");
        body.AppendLine();
        body.AppendLine($"You now have access to supporter benefits for {str}.");
        body.AppendLine();

        body.AppendLine("Use the following code in-game to redeem your benefits:");
        body.AppendLine(code);
        body.AppendLine();
        body.AppendLine("If you don't know how to redeem it: https://fluxis.flux.moe/wiki/donating#redeeming-your-code");
        body.AppendLine();

        const int per_month = 17;
        var runtime = TimeSpan.FromDays(amount / per_month * 31);
        body.AppendLine($"As a small fun fact, your donation keeps the servers running for about {runtime.Days} days, {runtime.Hours} hours and {runtime.Minutes} minutes.");

        body.AppendLine();
        body.AppendLine("If you have any questions, you can reply to this email. I will try to answer as soon as possible!");
        body.AppendLine();
        body.AppendLine("Have a great day!");
        body.AppendLine("- flustix");

        mail.SendMail(email, "Thank you for supporting fluXis!", body.ToString());
    }
}
