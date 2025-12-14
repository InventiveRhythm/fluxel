using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Extensions;
using fluxel.Database.Helpers;
using fluXis.Online.API.Payloads.Chat;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.Social.API.Chat;

public class CreateChannelRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/chat/channels";
    public HttpMethod Method => HttpMethod.Post;

    public IEnumerable<(string, string)> Validate(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryParseBody<ChatCreateChannelPayload>(out var payload))
            yield return ("_form", ResponseStrings.InvalidBodyJson);

        if (payload == null)
            yield break;

        interaction.AddCache("payload", payload);

        if (payload.TargetID is null)
            yield return ("target", ResponseStrings.FieldRequired);
    }

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetCache<ChatCreateChannelPayload>("payload", out var payload))
            throw new CacheMissingException("payload");

        if (!interaction.Cache.Users.TryGet(payload.TargetID!.Value, out var target))
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.UserNotFound);
            return;
        }

        if (!RelationHelper.Mutual(interaction.UserID, target.ID))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "You must follow each other to send direct messages.");
            return;
        }

        var arr = new List<long> { interaction.UserID, target.ID };
        arr.Sort();

        var name = $"private_{string.Join("-", arr)}";
        var channel = ChatHelper.GetChannel(name) ?? ChatHelper.CreateDirectChannel(name, arr[0], arr[1]);

        ChatHelper.AddToChannel(channel.Name, interaction.UserID);
        interaction.GetSocket()?.Client.AddToChatChannel(channel.ToAPI());

        await interaction.Reply(HttpStatusCode.OK, channel.Name);
    }
}
