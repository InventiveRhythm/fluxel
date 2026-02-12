using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluxel.Database.Helpers;
using fluxel.Models.Maps;
using fluxel.Models.Maps.Modding;
using fluXis.Online.API.Models.Maps.Modding;
using Midori.API.Components.Interfaces;
using Midori.Networking;

namespace fluxel.API.Routes.MapSets.Queue;

public class MapSetSubmitQueueRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/mapset/:id/submit";
    public HttpMethod Method => HttpMethod.Post;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetLongParameter("id", out var id))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("id", "long"));
            return;
        }

        var set = MapSetHelper.Get(id);

        if (set == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.MapSetNotFound);
            return;
        }

        if (set.CreatorID != interaction.User.ID)
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "You are not the creator of this mapset.");
            return;
        }

        if (set.Status >= MapStatus.Pure)
        {
            await interaction.ReplyMessage(HttpStatusCode.Conflict, "This mapset is already purified.");
            return;
        }

        if (set.Status == MapStatus.Pending)
        {
            await interaction.ReplyMessage(HttpStatusCode.Conflict, "This mapset is already in the queue.");
            return;
        }

        if (set.MapsList.Any(x => x.Mode is < 4 or > 8))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "A map in this mapset is not between 4 and 8 keys.");
            return;
        }

        var inQueueCount = MapSetHelper.InQueueByCount(interaction.UserID);

        if (inQueueCount >= MapSetHelper.MAX_MAPSETS_IN_QUEUE)
        {
            await interaction.ReplyMessage(HttpStatusCode.Conflict, $"You have reached the maximum number of mapsets in the queue. ({inQueueCount}/{MapSetHelper.MAX_MAPSETS_IN_QUEUE})");
            return;
        }

        set.QueueTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        set.QueueVotes = new List<ModQueueVote>();
        set.Status = MapStatus.Pending;
        MapSetHelper.Update(set);

        await interaction.Reply(HttpStatusCode.OK);

        MapSetHelper.CreateModAction(set.ID, interaction.UserID, APIModdingActionType.Submitted);
    }
}
