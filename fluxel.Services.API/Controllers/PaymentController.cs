using fluxel.Components;
using fluxel.Config;
using fluxel.Models.Monetize;
using Microsoft.Extensions.Logging;
using Midori.API.Attributes;
using Midori.API.Components;
using Midori.Networking;
using Midori.Utils.Extensions;
using Newtonsoft.Json;

namespace fluxel.Services.API.Controllers;

[Controller("/payment")]
public class PaymentController
{
    private readonly ILogger logger;
    private readonly ServerConfig config;
    private readonly Donations donations;

    public PaymentController(ILoggerFactory loggerFactory, ServerConfig config, Donations donations)
    {
        this.config = config;
        this.donations = donations;
        logger = loggerFactory.CreateLogger("KoFi");
    }

    [HttpRoute("/kofi/webhook", APIMethod.Post)]
    public APIReturn<object> KoFiWebhook([Source(ParameterSource.Form)] WebhookPayload data, [Source(ParameterSource.Form, "data")] string raw)
    {
        if (data.VerificationToken != config.KoFiSecret)
            return Returns.Message(HttpStatusCode.Forbidden, "Invalid verification token.");
        if (string.IsNullOrEmpty(data.MessageID))
            return Returns.Message(HttpStatusCode.BadRequest, "Invalid message ID.");
        if (string.IsNullOrEmpty(data.Email))
            return Returns.Message(HttpStatusCode.BadRequest, "Invalid E-Mail.");
        if (!(data.Amount?.TryParseDoubleInvariant(out var amount) ?? false))
            return Returns.Message(HttpStatusCode.BadRequest, "Invalid amount.");

        donations.RegisterPayment(new KoFiPayment
        {
            MessageID = data.MessageID!,
            Email = data.Email,
            Amount = amount,
            RawEvent = raw
        });

        logger.LogInformation($"Received {data.Amount}{data.Currency} from {data.Email} for message {data.MessageID}.");
        return Returns.Okay();
    }

    public class WebhookPayload
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
