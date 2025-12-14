using System.Net.Http;
using System.Threading.Tasks;
using fluxel.API.Components;
using fluxel.Constants;
using fluXis.Online.API.Models.Clubs;
using Midori.API.Components.Interfaces;
using Midori.Networking;
using Newtonsoft.Json;

namespace fluxel.API.Routes.Clubs.Members;

public class AddClubMemberRoute : IFluxelAPIRoute, INeedsAuthorization
{
    public string RoutePath => "/club/:clubid/members";
    public HttpMethod Method => HttpMethod.Put;

    public async Task Handle(FluxelAPIInteraction interaction)
    {
        if (!interaction.TryGetLongParameter("clubid", out var clubid))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidParameter("clubid", "long"));
            return;
        }

        if (!interaction.TryParseBody<Payload>(out var payload))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, ResponseStrings.InvalidBodyJson);
            return;
        }

        var club = interaction.Cache.GetClub(clubid);

        if (club == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.ClubNotFound);
            return;
        }

        if (payload.UserID != interaction.UserID)
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "You cannot add other people to clubs.");
            return;
        }

        if (club.JoinType != ClubJoinType.Open)
        {
            await interaction.ReplyMessage(HttpStatusCode.Forbidden, "This club is not open.");
            return;
        }

        var user = interaction.Cache.Users.Get(payload.UserID);

        if (user == null)
        {
            await interaction.ReplyMessage(HttpStatusCode.NotFound, ResponseStrings.UserNotFound);
            return;
        }

        if (club.IsInClub(user.ID))
        {
            await interaction.ReplyMessage(HttpStatusCode.BadRequest, "You already are in this club!");
            return;
        }

        club.AddMember(user.ID);
        await interaction.Reply(HttpStatusCode.Created);
    }

    private class Payload
    {
        [JsonProperty("member")]
        public long UserID { get; set; }
    }
}
