using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using fluxel.Database;
using fluxel.Models.Payment;
using Midori.Database;
using Midori.Utils;
using MongoDB.Bson.Serialization.Attributes;

namespace fluxel.Components;

public class Donations
{
    private readonly IDatabaseTable<KoFiPayment> collection;
    private readonly IDatabaseTable<LinkID> links;

    private readonly UserManager users;
    private readonly MailDelivery mail;

    public Donations(IDatabaseProvider db, UserManager users, MailDelivery mail)
    {
        this.users = users;
        this.mail = mail;
        collection = db.GetTable<KoFiPayment>("payments");
        links = db.GetTable<LinkID>("payment-links");
    }

    public void RegisterPayment(KoFiPayment payment)
    {
        collection.Add(payment);

        var user = users.GetByKoFiEmail(payment.Email);

        if (user is null)
            SendLink(payment.Email);
        else
            Update(user.ID);
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

    public void Update(long id)
    {
        var wasSupporterBefore = false;
        var currentlyActive = false;
        DateTime? endTime = null;
        var days = 0d;
        var total = 0d;

        var user = users.UpdateLocked(id, u =>
        {
            if (u.KoFiEmail is null)
                throw new Exception($"??? {u.ID} {u.Email} {u.Username}");

            var payments = collection.Find(x => x.Email.Equals(u.KoFiEmail, StringComparison.CurrentCultureIgnoreCase)).ToList();
            var unhandled = payments.Where(x => !x.Handled).ToList();
            total = unhandled.Sum(x => x.Amount);

            wasSupporterBefore = u.SupportEndTime != null;
            currentlyActive = u.IsSupporter;

            var now = DateTime.UtcNow;
            var time = u.SupportEndTime ?? now;

            if (time < now)
                time = now;

            days = total / 2d * 31;
            time = time.AddDays(days);
            endTime = u.SupportEndTime = time;

            foreach (var payment in payments)
            {
                payment.Handled = true;
                collection.Replace(p => p.MessageID == payment.MessageID, payment);
            }
        });

        var span = TimeSpan.FromDays(days);
        var months = span.Days / 31;
        var str = "";

        if (months >= 1f)
        {
            str += $"{months} month{(months > 1 ? "s" : "")}";

            var rest = span.Days - months * 31;
            if (rest > 0) str += $" {rest} day{(rest > 1 ? "s" : "")}";
        }
        else
            str = $"{span.Days} days";

        var body = new StringBuilder();
        body.AppendLine($"Hi {user.Username},");
        body.AppendLine();
        body.AppendLine(wasSupporterBefore ? "Thank you for supporting fluXis again!" : "Thank you for supporting fluXis!");
        body.AppendLine("The game runs without ads or forced payments because of people like you.");
        body.AppendLine();
        body.AppendLine($"You now have access to supporter benefits for{(currentlyActive ? " an extra" : "")} {str}. (until {endTime:dd MMMM yyyy hh:mm} UTC)");
        body.AppendLine();

        const int per_month = 25;
        var runtime = TimeSpan.FromDays(total / per_month * 31);
        body.AppendLine($"As a small fun fact, your donation keeps the servers running for about {runtime.Days} days, {runtime.Hours} hours and {runtime.Minutes} minutes.");

        body.AppendLine();
        body.AppendLine("If you have any questions, you can reply to this email. I will try to answer as soon as possible!");
        body.AppendLine();
        body.AppendLine("Have a great day!");
        body.AppendLine("- flustix");

        mail.SendMail(user.Email, "Thank you for supporting fluXis!", body.ToString(), user.ID, true);
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
