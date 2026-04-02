using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Models.Payment;
using fluXis.Utils;
using Midori.Logging;
using Midori.Networking;
using Newtonsoft.Json;

namespace fluxel.API.Routes.Payment;

public class KoFiWebhookRoute : IFluxelAPIRoute
{
    public string RoutePath => "/payment/kofi/webhook";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        using var reader = new StreamReader(interaction.Request.BodyStream);
        var form = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(form))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Invalid request.");
            return;
        }

        var parameters = form.Split('&');
        var data = new Dictionary<string, string>();

        foreach (var parameter in parameters)
        {
            var keyValue = parameter.Split('=');

            if (keyValue.Length == 2)
            {
                var key = Uri.UnescapeDataString(keyValue[0]);
                var value = Uri.UnescapeDataString(keyValue[1]);
                data[key] = value;
            }
        }

        if (data.TryGetValue("data", out var json))
        {
            var payload = JsonConvert.DeserializeObject<Payload>(json)!;

            if (payload.VerificationToken != ServerHost.Configuration.KoFiSecret)
            {
                await interaction.ReplyMessage(HttpStatusCode.Forbidden, "Invalid verification token.");
                return;
            }

            if (string.IsNullOrEmpty(payload.MessageID))
            {
                // SentrySdk.CaptureMessage($"Received Ko-Fi webhook with empty message ID. ({json})", SentryLevel.Error);
                await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Invalid message ID.");
                return;
            }

            if (string.IsNullOrEmpty(payload.Email))
            {
                // SentrySdk.CaptureMessage($"Received Ko-Fi webhook with empty email. ({json})", SentryLevel.Error);
                await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Invalid E-Mail.");
                return;
            }

            if (!payload.Amount.TryParseDoubleInvariant(out var amount))
            {
                // SentrySdk.CaptureMessage($"Received Ko-Fi webhook with invalid amount. ({json})", SentryLevel.Error);
                await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Invalid amount.");
                return;
            }

            Donations.RegisterPayment(new KoFiPayment
            {
                MessageID = payload.MessageID!,
                Email = payload.Email,
                Amount = amount,
                RawEvent = json
            });

            Logger.Log($"Received {payload.Amount}{payload.Currency} from {payload.Email} for message {payload.MessageID}.");
            await interaction.Reply(HttpStatusCode.OK);
        }
        else
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "Invalid event type.");
    }

    private class Payload
    {
        [JsonProperty("verification_token")]
        public string? VerificationToken { get; set; }

        [JsonProperty("message_id")]
        public string? MessageID { get; set; }

        [JsonProperty("amount")]
        public string? Amount { get; set; }

        [JsonProperty("currency")]
        public string? Currency { get; set; }

        [JsonProperty("email")]
        public string? Email { get; set; }
    }
}
