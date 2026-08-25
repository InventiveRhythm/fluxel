using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using fluxel.Database;
using fluxel.Models.Payment;
using Midori.Database;
using Midori.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Components;

public class Donations
{
    private readonly IDatabaseTable<KoFiPayment> collection;
    private readonly IDatabaseTable<LinkID> links;

    private readonly UserManager users;
    private readonly RedemptionManager redemptions;
    private readonly MailDelivery mail;

    public Donations(IDatabaseProvider db, UserManager users, MailDelivery mail, RedemptionManager redemptions)
    {
        this.users = users;
        this.mail = mail;
        this.redemptions = redemptions;
        collection = db.GetTable<KoFiPayment>("payments");
        links = db.GetTable<LinkID>("payment-links");
    }

    public void RegisterPayment(KoFiPayment payment)
    {
        collection.Add(payment);

        var rc = redemptions.Create(new BsonDocument().Add(RedemptionCode.DONATE_AMOUNT, payment.Amount));
        sendCodeLink(payment.Email, payment.Amount, rc.Code);
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

        mail.SendMail(email, "Thank you for supporting fluXis!", body.ToString(), -1, true);
    }

    public bool Connect(string code, long user, [NotNullWhen(false)] out string? error)
    {
        error = null;

        var link = links.Find(x => x.Code.Equals(code)).FirstOrDefault();

        if (link is null)
        {
            error = "Invalid link code.";
            return false;
        }

        users.UpdateLocked(user, u => u.KoFiEmail = link.Email);
        return true;
    }

    public void SendLink(string target)
    {
        var link = links.Find(x => x.Email.Equals(target, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

        if (link is null)
        {
            link = new LinkID(target);
            links.Add(link);
        }

        var body = new StringBuilder();
        body.AppendLine("Hi! Thank you for supporting fluXis!");
        body.AppendLine();
        body.AppendLine("But it seems like your Ko-fi email is not linked to a fluXis account!");
        body.AppendLine("Please click on the following link to connect your account:");
        body.AppendLine();
        body.AppendLine($"https://auth.flux.moe/link/kofi?code={link.Code}");
        mail.SendMail(target, "Link your Ko-fi account to fluXis!", body.ToString(), 0);
    }

    private class LinkID
    {
        [BsonId]
        public string Email { get; init; }

        [BsonElement("code")]
        public string Code { get; init; }

        public LinkID(string mail)
        {
            Email = mail.ToLowerInvariant();
            Code = RandomizeUtils.GenerateRandomString(12);
        }
    }
}
